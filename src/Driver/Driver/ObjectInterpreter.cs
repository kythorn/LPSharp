using System.Text;

namespace Driver;

/// <summary>
/// Object-centric interpreter for executing LPC code within a MudObject's context.
/// This is the authentic LPMud execution model where all code runs within an object.
/// Variables and functions are accessed from the object's state and program.
/// </summary>
public class ObjectInterpreter
{
    private readonly EfunRegistry _efuns;
    private readonly ObjectManager _objectManager;

    /// <summary>
    /// The current object whose code is being executed.
    /// This is the "this_object()" context.
    /// </summary>
    private MudObject _currentObject;

    /// <summary>
    /// Call stack for tracking function calls across objects.
    /// Top of stack is the most recent caller.
    /// Used for previous_object() efun.
    /// </summary>
    private readonly Stack<MudObject> _callStack = new();

    /// <summary>
    /// Local variable scopes for function parameters and local variables.
    /// Stack of dictionaries - one per function call depth.
    /// Top of stack is the current function's local scope.
    /// </summary>
    private readonly Stack<Dictionary<string, object?>> _localScopes = new();

    /// <summary>
    /// Stack tracking which program each executing function belongs to.
    /// Used for correct :: (parent call) behavior in inheritance chains.
    /// When function A from program X calls ::foo(), we need to search
    /// from X's inheritance chain, not from _currentObject's program.
    /// </summary>
    private readonly Stack<LpcProgram> _executingPrograms = new();

    #region Error Tracking

    /// <summary>
    /// Current file being executed (for error messages).
    /// </summary>
    private string _currentFile = "";

    /// <summary>
    /// Current line being executed (for error messages).
    /// </summary>
    private int _currentLine;

    /// <summary>
    /// Stack of (file, function) for building stack traces.
    /// </summary>
    private readonly Stack<(string File, string Function, int Line)> _traceStack = new();

    /// <summary>
    /// Create an error with file/line context.
    /// </summary>
    private LpcRuntimeException RuntimeError(string message)
    {
        return new LpcRuntimeException(message, _currentFile, _currentLine, BuildStackTrace());
    }

    /// <summary>
    /// Create an error with file/line context from an expression.
    /// </summary>
    private LpcRuntimeException RuntimeError(string message, Expression expr)
    {
        return new LpcRuntimeException(message, _currentFile, expr.Line, BuildStackTrace());
    }

    /// <summary>
    /// Create an error with file/line context from a statement.
    /// </summary>
    private LpcRuntimeException RuntimeError(string message, Statement stmt)
    {
        return new LpcRuntimeException(message, _currentFile, stmt.Line, BuildStackTrace());
    }

    /// <summary>
    /// Build a stack trace string from the trace stack.
    /// </summary>
    private string BuildStackTrace()
    {
        if (_traceStack.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("Stack trace:");
        foreach (var (file, func, line) in _traceStack.Reverse())
        {
            sb.AppendLine($"  {file}:{line} in {func}()");
        }
        return sb.ToString();
    }

    #endregion

    #region Execution Limits

    /// <summary>
    /// Current instruction count for this execution context.
    /// Reset at the start of each top-level command execution.
    /// </summary>
    private int _instructionCount;

    /// <summary>
    /// Maximum instructions allowed per execution context.
    /// Prevents infinite loops from hanging the game.
    /// Default: 1,000,000 instructions (roughly 1-2 seconds of execution)
    /// </summary>
    public int MaxInstructions { get; set; } = 1_000_000;

    /// <summary>
    /// Maximum recursion depth (function call depth).
    /// Prevents stack overflow from infinite recursion.
    /// Default: 100 levels deep
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 100;

    /// <summary>
    /// Whether execution limits are enabled.
    /// Can be disabled for testing or specific admin commands.
    /// </summary>
    public bool LimitsEnabled { get; set; } = true;

    /// <summary>
    /// Reset the instruction counter. Called at the start of each command execution.
    /// </summary>
    public void ResetInstructionCount()
    {
        _instructionCount = 0;
    }

    /// <summary>
    /// Increment the instruction counter and check limits.
    /// Called for each statement and expression evaluation.
    /// </summary>
    private void CountInstruction()
    {
        if (!LimitsEnabled) return;

        _instructionCount++;
        if (_instructionCount > MaxInstructions)
        {
            throw new ExecutionLimitException(
                $"Execution limit exceeded: {MaxInstructions} instructions. " +
                "This usually indicates an infinite loop.",
                _currentFile, _currentLine);
        }
    }

    /// <summary>
    /// Check recursion depth limit.
    /// </summary>
    private void CheckRecursionDepth()
    {
        if (!LimitsEnabled) return;

        if (_localScopes.Count > MaxRecursionDepth)
        {
            throw new ExecutionLimitException(
                $"Recursion limit exceeded: {MaxRecursionDepth} levels. " +
                "This usually indicates infinite recursion.");
        }
    }

    #endregion

    #region Permission System

    /// <summary>
    /// Get the access level of the current player executing code.
    /// Returns Admin if no execution context (system-level operations like tests).
    /// Returns Guest if there's a context but no valid session.
    /// </summary>
    private AccessLevel GetCurrentAccessLevel()
    {
        var context = ExecutionContext.Current;

        // No execution context = system-level operation (tests, object init, etc.)
        // These are allowed unrestricted access
        if (context == null)
        {
            return AccessLevel.Admin;
        }

        // Execution context without player object = Guest
        if (context.PlayerObject == null)
        {
            return AccessLevel.Guest;
        }

        // Find the session for this player
        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            // No game loop = probably a test, allow unrestricted
            return AccessLevel.Admin;
        }

        var session = gameLoop.GetAllSessions()
            .FirstOrDefault(s => s.PlayerObject == context.PlayerObject);

        return session?.AccessLevel ?? AccessLevel.Guest;
    }

    /// <summary>
    /// Get the username of the current player executing code.
    /// Returns null if no player context.
    /// </summary>
    private string? GetCurrentUsername()
    {
        var context = ExecutionContext.Current;
        if (context?.PlayerObject == null)
        {
            return null;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return null;
        }

        var session = gameLoop.GetAllSessions()
            .FirstOrDefault(s => s.PlayerObject == context.PlayerObject);

        return session?.AuthenticatedUsername;
    }

    /// <summary>
    /// Check if the path is within the /secure/ directory (admin-only).
    /// </summary>
    private bool IsSecurePath(string mudlibPath)
    {
        var normalized = mudlibPath.TrimStart('/').ToLowerInvariant();
        return normalized.StartsWith("secure/") || normalized == "secure";
    }

    /// <summary>
    /// Check if the path is within a wizard's home directory.
    /// </summary>
    private bool IsWizardHomePath(string mudlibPath, string username)
    {
        var normalized = mudlibPath.TrimStart('/').ToLowerInvariant();
        var homePath = $"wizards/{username.ToLowerInvariant()}/";
        return normalized.StartsWith(homePath) || normalized == $"wizards/{username.ToLowerInvariant()}";
    }

    /// <summary>
    /// Check if the path is in another wizard's private directory.
    /// </summary>
    private bool IsOtherWizardHomePath(string mudlibPath, string currentUsername)
    {
        var normalized = mudlibPath.TrimStart('/').ToLowerInvariant();
        if (!normalized.StartsWith("wizards/"))
        {
            return false;
        }

        // Extract wizard name from path: wizards/{name}/...
        var parts = normalized.Split('/', 3);
        if (parts.Length < 2)
        {
            return false;
        }

        var wizardName = parts[1];
        return !string.Equals(wizardName, currentUsername, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the current user can access a path for read or write operations.
    /// </summary>
    /// <param name="mudlibPath">The path relative to mudlib root (e.g., "/std/object.c")</param>
    /// <param name="isWrite">True for write operations, false for read</param>
    /// <returns>True if access is allowed</returns>
    private bool CanAccessPath(string mudlibPath, bool isWrite)
    {
        var accessLevel = GetCurrentAccessLevel();
        var username = GetCurrentUsername();

        // Admin bypasses all permission checks
        if (accessLevel >= AccessLevel.Admin)
        {
            return true;
        }

        // /secure/ is admin-only
        if (IsSecurePath(mudlibPath))
        {
            return false;
        }

        // Players cannot access any filesystem
        if (accessLevel < AccessLevel.Wizard)
        {
            return false;
        }

        // Wizard level
        if (username == null)
        {
            return false;
        }

        // Wizards can always access their own home directory
        if (IsWizardHomePath(mudlibPath, username))
        {
            return true;
        }

        // Other wizard's home directories are private
        if (IsOtherWizardHomePath(mudlibPath, username))
        {
            return false;
        }

        // For public paths: wizards can read but not write
        return !isWrite;
    }

    /// <summary>
    /// Require access to a path, throwing an exception if denied.
    /// </summary>
    private void RequirePathAccess(string mudlibPath, string operation, bool isWrite = false)
    {
        if (!CanAccessPath(mudlibPath, isWrite))
        {
            var accessLevel = GetCurrentAccessLevel();
            throw new EfunException($"Permission denied: {operation} on '{mudlibPath}' (access level: {accessLevel})");
        }
    }

    /// <summary>
    /// Require minimum access level for an operation.
    /// </summary>
    private void RequireAccessLevel(AccessLevel required, string operation)
    {
        var current = GetCurrentAccessLevel();
        if (current < required)
        {
            throw new EfunException($"Permission denied: {operation} requires {required} access (you have {current})");
        }
    }

    #endregion

    public ObjectInterpreter(ObjectManager objectManager, TextWriter? output = null)
    {
        _objectManager = objectManager;
        _efuns = new EfunRegistry(output);
        _currentObject = null!; // Will be set before execution

        // Register object-specific efuns
        RegisterObjectEfuns();
    }

    /// <summary>
    /// Register object-specific efuns that need access to ObjectManager and interpreter state.
    /// </summary>
    private void RegisterObjectEfuns()
    {
        _efuns.Register("clone_object", CloneObjectEfun);
        _efuns.Register("this_object", ThisObjectEfun);
        _efuns.Register("load_object", LoadObjectEfun);
        _efuns.Register("find_object", FindObjectEfun);
        _efuns.Register("destruct", DestructEfun);
        _efuns.Register("call_other", CallOtherEfun);
        _efuns.Register("move_object", MoveObjectEfun);
        _efuns.Register("present", PresentEfun);

        // Object metadata efuns
        _efuns.Register("object_name", ObjectNameEfun);
        _efuns.Register("file_name", FileNameEfun);
        _efuns.Register("previous_object", PreviousObjectEfun);

        // Shadow efuns
        _efuns.Register("shadow", ShadowEfun);
        _efuns.Register("query_shadowing", QueryShadowingEfun);
        _efuns.Register("unshadow", UnshadowEfun);

        // Living/Interactive efuns
        _efuns.Register("set_living", SetLivingEfun);
        _efuns.Register("living", LivingEfun);
        _efuns.Register("set_living_name", SetLivingNameEfun);
        _efuns.Register("query_living_name", QueryLivingNameEfun);
        _efuns.Register("interactive", InteractiveEfun);
        _efuns.Register("find_living", FindLivingEfun);
        _efuns.Register("find_player", FindPlayerEfun);
        _efuns.Register("users", UsersEfun);
        _efuns.Register("linkdead_users", LinkdeadUsersEfun);
        _efuns.Register("query_linkdead", QueryLinkdeadEfun);

        // Heartbeat efuns
        _efuns.Register("set_heart_beat", SetHeartBeatEfun);
        _efuns.Register("query_heart_beat", QueryHeartBeatEfun);

        // Callout efuns
        _efuns.Register("call_out", CallOutEfun);
        _efuns.Register("remove_call_out", RemoveCallOutEfun);
        _efuns.Register("find_call_out", FindCallOutEfun);

        // Array callback efuns (need interpreter access)
        _efuns.Register("filter_array", FilterArrayEfun);
        _efuns.Register("map_array", MapArrayEfun);

        // File I/O efuns
        _efuns.Register("read_file", ReadFileEfun);
        _efuns.Register("write_file", WriteFileEfun);
        _efuns.Register("file_size", FileSizeEfun);
        _efuns.Register("rm", RmEfun);

        // Object persistence efuns
        _efuns.Register("save_object", SaveObjectEfun);
        _efuns.Register("restore_object", RestoreObjectEfun);

        // Hot-reload efuns
        _efuns.Register("update", UpdateEfun);
        _efuns.Register("inherits", InheritsEfun);

        // Input handling efuns
        _efuns.Register("input_to", InputToEfun);

        // Action system efuns
        _efuns.Register("add_action", AddActionEfun);
        _efuns.Register("query_verb", QueryVerbEfun);
        _efuns.Register("notify_fail", NotifyFailEfun);
        _efuns.Register("enable_commands", EnableCommandsEfun);
        _efuns.Register("disable_commands", DisableCommandsEfun);
        _efuns.Register("command", CommandEfun);

        // Additional object efuns
        _efuns.Register("clonep", ClonepEfun);
        _efuns.Register("say", SayEfun);

        // Access level efuns
        _efuns.Register("set_access_level", SetAccessLevelEfun);
        _efuns.Register("query_access_level", QueryAccessLevelEfun);
        _efuns.Register("homedir", HomedirEfun);

        // Server control efuns (Admin only)
        _efuns.Register("shutdown", ShutdownEfun);

        // Error handling efuns
        _efuns.Register("throw", ThrowEfun);

        // Logging efuns
        _efuns.Register("syslog", SyslogEfun);

        // Directory listing efun (for ls command)
        _efuns.Register("get_dir", GetDirEfun);

        // Alias management efuns
        _efuns.Register("query_aliases", QueryAliasesEfun);
        _efuns.Register("query_alias", QueryAliasEfun);
        _efuns.Register("set_alias", SetAliasEfun);
        _efuns.Register("remove_alias", RemoveAliasEfun);
        _efuns.Register("reset_aliases", ResetAliasesEfun);
    }

    /// <summary>
    /// Call an efun by name. Useful for testing.
    /// </summary>
    public object CallEfun(string name, List<object> args)
    {
        if (_efuns.TryGet(name, out var efun) && efun != null)
        {
            return efun(args);
        }
        throw new ObjectInterpreterException($"Unknown efun: {name}");
    }

    /// <summary>
    /// Execute code in the context of a specific object.
    /// All variable and function lookups use the object's state.
    /// </summary>
    public object? ExecuteInObject(MudObject obj, Statement stmt)
    {
        var previousObject = _currentObject;
        _currentObject = obj;

        try
        {
            return Execute(stmt);
        }
        finally
        {
            _currentObject = previousObject;
        }
    }

    /// <summary>
    /// Evaluate an expression in the context of a specific object.
    /// </summary>
    public object EvaluateInObject(MudObject obj, Expression expr)
    {
        var previousObject = _currentObject;
        _currentObject = obj;

        try
        {
            return Evaluate(expr);
        }
        finally
        {
            _currentObject = previousObject;
        }
    }

    /// <summary>
    /// Call a function on an object.
    /// Manages the call stack for previous_object() tracking.
    /// If the target has a shadow, the shadow gets first chance to handle the call.
    /// </summary>
    public object? CallFunctionOnObject(MudObject target, string functionName, List<object> args)
    {
        return CallFunctionOnObjectInternal(target, functionName, args, checkShadow: true);
    }

    /// <summary>
    /// Call a function on an object, optionally bypassing shadow check.
    /// Used internally to prevent infinite recursion when shadows call through to original.
    /// </summary>
    private object? CallFunctionOnObjectInternal(MudObject target, string functionName, List<object> args, bool checkShadow)
    {
        // Check for shadow first (unless bypassed)
        if (checkShadow && target.ShadowedBy != null)
        {
            var shadow = target.ShadowedBy;
            var shadowFunc = shadow.Program.FindFunction(functionName);
            if (shadowFunc != null)
            {
                // Shadow has this function - call it instead
                return CallFunctionOnObjectInternal(shadow, functionName, args, checkShadow: false);
            }
        }

        var (func, owningProgram) = target.Program.FindFunctionWithProgram(functionName);
        if (func == null)
        {
            throw new ObjectInterpreterException($"Function '{functionName}' not found in object {target.ObjectName}");
        }

        // Push caller onto stack
        _callStack.Push(_currentObject);

        var previousObject = _currentObject;
        _currentObject = target;

        try
        {
            return CallUserFunctionWithProgram(func, args, owningProgram);
        }
        finally
        {
            _currentObject = previousObject;
            _callStack.Pop();
        }
    }

    /// <summary>
    /// Call a function on an object during initialization (for create() lifecycle hook).
    /// Does not manage call stack since there's no caller during object creation.
    /// </summary>
    public object? CallFunctionOnObjectInit(MudObject target, string functionName)
    {
        var (func, owningProgram) = target.Program.FindFunctionWithProgram(functionName);
        if (func == null)
        {
            return null; // Function doesn't exist, which is okay
        }

        var previousObject = _currentObject;
        _currentObject = target;

        try
        {
            return CallUserFunctionWithProgram(func, new List<object>(), owningProgram);
        }
        finally
        {
            _currentObject = previousObject;
        }
    }

    #region Statement Execution

    private object? Execute(Statement stmt)
    {
        // Track current line for error messages
        _currentLine = stmt.Line;

        // Count each statement execution for limit checking
        CountInstruction();

        return stmt switch
        {
            BlockStatement block => ExecuteBlock(block),
            ExpressionStatement exprStmt => Evaluate(exprStmt.Expression),
            IfStatement ifStmt => ExecuteIf(ifStmt),
            WhileStatement whileStmt => ExecuteWhile(whileStmt),
            ForStatement forStmt => ExecuteFor(forStmt),
            ForEachStatement foreachStmt => ExecuteForeach(foreachStmt),
            SwitchStatement switchStmt => ExecuteSwitch(switchStmt),
            ReturnStatement ret => ExecuteReturn(ret),
            BreakStatement => throw new BreakException(),
            ContinueStatement => throw new ContinueException(),
            VariableDeclaration varDecl => ExecuteVariableDeclaration(varDecl),
            _ => throw RuntimeError($"Unknown statement type: {stmt.GetType().Name}", stmt)
        };
    }

    private object? ExecuteVariableDeclaration(VariableDeclaration varDecl)
    {
        // Determine initial value
        object? initialValue = varDecl.Initializer != null
            ? Evaluate(varDecl.Initializer)
            : GetDefaultValue(varDecl.Type);

        // If we're inside a function (local scope exists), add to local scope
        if (_localScopes.Count > 0)
        {
            _localScopes.Peek()[varDecl.Name] = initialValue;
        }
        else
        {
            // Top-level: set on object (existing behavior for object variables)
            _currentObject.SetVariable(varDecl.Name, initialValue);
        }

        return null;
    }

    /// <summary>
    /// Get the default value for a type.
    /// In LPC, integers default to 0, strings to empty string.
    /// </summary>
    private static object GetDefaultValue(string type)
    {
        return type switch
        {
            "string" => "",
            _ => 0L  // int, object, mixed, void, etc. default to 0
        };
    }

    private object? ExecuteBlock(BlockStatement block)
    {
        object? lastValue = null;
        foreach (var stmt in block.Statements)
        {
            lastValue = Execute(stmt);
        }
        return lastValue;
    }

    private object? ExecuteIf(IfStatement stmt)
    {
        var condition = Evaluate(stmt.Condition);

        if (IsTrue(condition))
        {
            return Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch != null)
        {
            return Execute(stmt.ElseBranch);
        }

        return null;
    }

    private object? ExecuteWhile(WhileStatement stmt)
    {
        object? lastValue = null;

        while (IsTrue(Evaluate(stmt.Condition)))
        {
            try
            {
                lastValue = Execute(stmt.Body);
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }

        return lastValue;
    }

    private object? ExecuteFor(ForStatement stmt)
    {
        object? lastValue = null;

        // Initialize
        if (stmt.Init != null)
        {
            Evaluate(stmt.Init);
        }

        // Loop
        while (stmt.Condition == null || IsTrue(Evaluate(stmt.Condition)))
        {
            try
            {
                lastValue = Execute(stmt.Body);
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                // Fall through to increment
            }

            // Increment
            if (stmt.Increment != null)
            {
                Evaluate(stmt.Increment);
            }
        }

        return lastValue;
    }

    private object? ExecuteSwitch(SwitchStatement stmt)
    {
        var switchValue = Evaluate(stmt.Value);
        object? lastValue = null;
        bool matched = false;
        int defaultIndex = -1;

        // First, find if there's a matching case or a default
        for (int i = 0; i < stmt.Cases.Count; i++)
        {
            var switchCase = stmt.Cases[i];

            if (switchCase.Value == null)
            {
                // This is the default case, remember its position
                defaultIndex = i;
            }
            else if (!matched)
            {
                var caseValue = Evaluate(switchCase.Value);
                if (ValuesEqual(switchValue, caseValue))
                {
                    matched = true;
                }
            }

            // If we've matched, execute this case's statements (fall-through behavior)
            if (matched)
            {
                try
                {
                    foreach (var statement in switchCase.Statements)
                    {
                        lastValue = Execute(statement);
                    }
                }
                catch (BreakException)
                {
                    // Break exits the switch
                    return lastValue;
                }
            }
        }

        // If no case matched but there's a default, execute from default onwards
        if (!matched && defaultIndex >= 0)
        {
            for (int i = defaultIndex; i < stmt.Cases.Count; i++)
            {
                var switchCase = stmt.Cases[i];
                try
                {
                    foreach (var statement in switchCase.Statements)
                    {
                        lastValue = Execute(statement);
                    }
                }
                catch (BreakException)
                {
                    return lastValue;
                }
            }
        }

        return lastValue;
    }

    /// <summary>
    /// Compare two values for equality (used in switch).
    /// </summary>
    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        // Handle int/long comparison
        if ((a is int or long) && (b is int or long))
        {
            return Convert.ToInt64(a) == Convert.ToInt64(b);
        }
        if (a is string sa && b is string sb) return sa == sb;
        return a.Equals(b);
    }

    private object? ExecuteForeach(ForEachStatement stmt)
    {
        var collection = Evaluate(stmt.Collection);
        object? lastValue = null;

        // Get items to iterate over
        IEnumerable<object?> items;
        if (collection is List<object> list)
        {
            items = list;
        }
        else if (collection is Dictionary<object, object> mapping)
        {
            // Iterate over keys for mappings
            items = mapping.Keys;
        }
        else if (collection is string str)
        {
            // Iterate over characters for strings
            items = str.Select(c => (object?)c.ToString());
        }
        else
        {
            throw new ObjectInterpreterException($"Cannot iterate over type: {collection?.GetType().Name ?? "null"}");
        }

        // Create a local scope for the loop variable if needed
        bool createdScope = false;
        if (_localScopes.Count == 0)
        {
            _localScopes.Push(new Dictionary<string, object?>());
            createdScope = true;
        }

        try
        {
            var currentScope = _localScopes.Peek();
            foreach (var item in items)
            {
                // Set the loop variable
                currentScope[stmt.Variable] = item;

                try
                {
                    lastValue = Execute(stmt.Body);
                }
                catch (BreakException)
                {
                    break;
                }
                catch (ContinueException)
                {
                    continue;
                }
            }
        }
        finally
        {
            if (createdScope)
            {
                _localScopes.Pop();
            }
        }

        return lastValue;
    }

    private object? ExecuteReturn(ReturnStatement stmt)
    {
        if (stmt.Value != null)
        {
            var value = Evaluate(stmt.Value);
            throw new ReturnException(value);
        }

        throw new ReturnException(0); // LPC returns 0 for void functions
    }

    #endregion

    #region Expression Evaluation

    private object Evaluate(Expression expr)
    {
        // Track current line for error messages
        _currentLine = expr.Line;

        // Count each expression evaluation for limit checking
        CountInstruction();

        return expr switch
        {
            NumberLiteral num => num.Value,
            StringLiteral str => str.Value,
            ArrayLiteral arr => EvaluateArrayLiteral(arr),
            MappingLiteral map => EvaluateMappingLiteral(map),
            Identifier id => EvaluateIdentifier(id),
            BinaryOp bin => EvaluateBinaryOp(bin),
            UnaryOp unary => EvaluateUnaryOp(unary),
            GroupedExpression grouped => Evaluate(grouped.Inner),
            TernaryOp ternary => EvaluateTernaryOp(ternary),
            Assignment assign => EvaluateAssignment(assign),
            CompoundAssignment compound => EvaluateCompoundAssignment(compound),
            FunctionCall call => EvaluateFunctionCall(call),
            ArrowCall arrow => EvaluateArrowCall(arrow),
            IndexExpression idx => EvaluateIndexExpression(idx),
            CatchExpression catchExpr => EvaluateCatch(catchExpr),
            _ => throw RuntimeError($"Unknown expression type: {expr.GetType().Name}", expr)
        };
    }

    /// <summary>
    /// Evaluate catch(expr) - returns 0 on success, error string on exception.
    /// </summary>
    private object EvaluateCatch(CatchExpression expr)
    {
        try
        {
            Evaluate(expr.Body);
            return 0L; // Success
        }
        catch (LpcThrowException ex)
        {
            // Explicit throw() - return the thrown value as error
            return ex.ThrownValue is string s ? s : ex.Message;
        }
        catch (LpcRuntimeException ex)
        {
            // Runtime error - return error message
            return ex.Message;
        }
        catch (EfunException ex)
        {
            // Efun error - return error message
            return ex.Message;
        }
        catch (ObjectInterpreterException ex)
        {
            // Interpreter error - return error message
            return ex.Message;
        }
        catch (ReturnException)
        {
            // return inside catch is allowed
            throw;
        }
        catch (BreakException)
        {
            // break inside catch is allowed
            throw;
        }
        catch (ContinueException)
        {
            // continue inside catch is allowed
            throw;
        }
        catch (ExecutionLimitException)
        {
            // Don't catch execution limits - they need to propagate
            throw;
        }
        catch (Exception ex)
        {
            // Catch-all for unexpected errors
            return $"*Unexpected error: {ex.Message}*";
        }
    }

    private object EvaluateArrayLiteral(ArrayLiteral arr)
    {
        var elements = new List<object>();
        foreach (var element in arr.Elements)
        {
            elements.Add(Evaluate(element));
        }
        return elements;
    }

    private object EvaluateMappingLiteral(MappingLiteral map)
    {
        var dict = new Dictionary<object, object>();
        foreach (var (keyExpr, valueExpr) in map.Entries)
        {
            var key = Evaluate(keyExpr);
            var value = Evaluate(valueExpr);
            dict[key] = value;
        }
        return dict;
    }

    private object EvaluateIndexExpression(IndexExpression expr)
    {
        var target = Evaluate(expr.Target);
        var index = Evaluate(expr.Index);

        if (target is string str)
        {
            if (index is not int i)
            {
                throw new ObjectInterpreterException("String index must be an integer");
            }
            if (i < 0 || i >= str.Length)
            {
                throw new ObjectInterpreterException($"String index {i} out of bounds (length {str.Length})");
            }
            return (int)str[i]; // Return character as int (LPC convention)
        }

        if (target is List<object> list)
        {
            if (index is not int i)
            {
                throw new ObjectInterpreterException("Array index must be an integer");
            }
            if (i < 0 || i >= list.Count)
            {
                throw new ObjectInterpreterException($"Array index {i} out of bounds (size {list.Count})");
            }
            return list[i];
        }

        if (target is Dictionary<object, object> dict)
        {
            if (dict.TryGetValue(index, out var value))
            {
                return value;
            }
            return 0L; // LPC returns 0 for missing mapping keys
        }

        throw new ObjectInterpreterException($"Cannot index into {target?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Evaluate arrow call: obj->func(args)
    /// This is syntactic sugar for call_other(obj, "func", args...)
    /// </summary>
    private object EvaluateArrowCall(ArrowCall arrow)
    {
        var target = Evaluate(arrow.Target);

        if (target is not MudObject targetObj)
        {
            // Check for 0 (null object) - could be int or long
            if ((target is int i && i == 0) || (target is long l && l == 0))
            {
                return 0L; // Calling on 0 returns 0 (LPC convention)
            }
            throw new ObjectInterpreterException($"Arrow call target must be an object, got {target?.GetType().Name ?? "null"}");
        }

        // Evaluate arguments
        var args = new List<object>();
        foreach (var arg in arrow.Arguments)
        {
            args.Add(Evaluate(arg));
        }

        // Find the function and check visibility (arrow is just syntactic sugar for call_other)
        var func = targetObj.FindFunction(arrow.FunctionName);
        if (func == null)
        {
            return 0L; // Function not found
        }

        // Security check: private, protected, and static functions cannot be called via arrow/call_other
        if ((func.Visibility & (FunctionVisibility.Private | FunctionVisibility.Protected | FunctionVisibility.Static)) != 0)
        {
            return 0L; // Act as if function doesn't exist
        }

        // Call the function on the target object
        try
        {
            return CallFunctionOnObject(targetObj, arrow.FunctionName, args) ?? 0L;
        }
        catch (ReturnException ret)
        {
            return ret.Value ?? 0L;
        }
    }

    private object EvaluateIdentifier(Identifier id)
    {
        // Check local scope first (function parameters/locals)
        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(id.Name, out var localValue))
        {
            return localValue ?? 0;
        }

        // Then check object variables
        var value = _currentObject.GetVariable(id.Name);
        return value ?? 0; // Default to 0 if null
    }

    private object EvaluateAssignment(Assignment expr)
    {
        var value = Evaluate(expr.Value);

        // Check if it's a local variable
        if (_localScopes.Count > 0 && _localScopes.Peek().ContainsKey(expr.Name))
        {
            _localScopes.Peek()[expr.Name] = value;
        }
        else
        {
            // It's an object variable
            _currentObject.SetVariable(expr.Name, value);
        }

        return value;
    }

    private object EvaluateCompoundAssignment(CompoundAssignment expr)
    {
        // Get current value (check local scope first)
        object? currentValue;
        bool isLocal = false;

        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(expr.Name, out var localVal))
        {
            currentValue = localVal;
            isLocal = true;
        }
        else
        {
            currentValue = _currentObject.GetVariable(expr.Name);
        }

        var rightValue = Evaluate(expr.Value);

        // Handle string concatenation for +=
        if (expr.Operator == BinaryOperator.Add && currentValue is string leftStr)
        {
            var result = leftStr + ToStr(rightValue);
            if (isLocal)
            {
                _localScopes.Peek()[expr.Name] = result;
            }
            else
            {
                _currentObject.SetVariable(expr.Name, result);
            }
            return result;
        }

        // Integer operations
        var left_i = ToInt(currentValue);
        var right_i = ToInt(rightValue);

        var newValue = expr.Operator switch
        {
            BinaryOperator.Add => left_i + right_i,
            BinaryOperator.Subtract => left_i - right_i,
            BinaryOperator.Multiply => left_i * right_i,
            BinaryOperator.Divide => right_i != 0 ? left_i / right_i
                : throw new ObjectInterpreterException("Division by zero"),
            BinaryOperator.Modulo => right_i != 0 ? left_i % right_i
                : throw new ObjectInterpreterException("Modulo by zero"),
            BinaryOperator.BitwiseAnd => left_i & right_i,
            BinaryOperator.BitwiseOr => left_i | right_i,
            BinaryOperator.BitwiseXor => left_i ^ right_i,
            BinaryOperator.LeftShift => left_i << (int)right_i,
            BinaryOperator.RightShift => left_i >> (int)right_i,
            _ => throw new ObjectInterpreterException($"Unsupported compound assignment operator: {expr.Operator}")
        };

        if (isLocal)
        {
            _localScopes.Peek()[expr.Name] = newValue;
        }
        else
        {
            _currentObject.SetVariable(expr.Name, newValue);
        }
        return newValue;
    }

    private object EvaluateFunctionCall(FunctionCall expr)
    {
        // Special handling for sscanf - needs raw variable identifiers
        if (expr.Name == "sscanf")
        {
            return EvaluateSscanf(expr);
        }

        // Evaluate all arguments first
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToList();

        // Handle parent function call (::function())
        if (expr.IsParentCall)
        {
            // For parent calls, we need to find the parent relative to the program
            // where the calling function is defined, not relative to _currentObject.
            // This is critical for correct behavior with multi-level inheritance.
            LpcProgram searchFrom;
            if (_executingPrograms.Count > 0)
            {
                // Use the program of the currently executing function
                searchFrom = _executingPrograms.Peek();
            }
            else
            {
                // No function context - use object's program (shouldn't happen normally)
                searchFrom = _currentObject.Program;
            }

            var parentFunc = searchFrom.FindParentFunction(expr.Name);
            if (parentFunc == null)
            {
                throw RuntimeError(
                    $"Parent function '{expr.Name}' not found in inheritance chain", expr);
            }

            // Find which program owns this parent function for correct nested parent calls
            var (_, owningProgram) = searchFrom.InheritedPrograms
                .Select(p => p.FindFunctionWithProgram(expr.Name))
                .FirstOrDefault(r => r.Function != null);

            return CallUserFunctionWithProgram(parentFunc, args, owningProgram) ?? 0;
        }

        // Check in current object's program (including inherited functions)
        var (objectFunc, funcProgram) = _currentObject.Program.FindFunctionWithProgram(expr.Name);
        if (objectFunc != null)
        {
            return CallUserFunctionWithProgram(objectFunc, args, funcProgram) ?? 0;
        }

        // Check for efun
        if (_efuns.TryGet(expr.Name, out var efun) && efun != null)
        {
            try
            {
                return efun(args);
            }
            catch (EfunException ex)
            {
                throw RuntimeError(ex.Message, expr);
            }
        }

        throw RuntimeError($"Unknown function '{expr.Name}' in {_currentObject.ObjectName}", expr);
    }

    /// <summary>
    /// Special handling for sscanf() which needs to assign to variables.
    /// sscanf(str, format, var1, var2, ...) - Parse a string according to format.
    /// Format specifiers: %s (string), %d (int), %*s (skip string), %*d (skip int)
    /// Returns the number of variables successfully assigned.
    /// </summary>
    private object EvaluateSscanf(FunctionCall expr)
    {
        if (expr.Arguments.Count < 2)
        {
            throw new ObjectInterpreterException("sscanf() requires at least 2 arguments");
        }

        // Evaluate only the first two arguments (string and format)
        var inputStr = Evaluate(expr.Arguments[0]) as string
            ?? throw new ObjectInterpreterException("sscanf() first argument must be a string");
        var formatStr = Evaluate(expr.Arguments[1]) as string
            ?? throw new ObjectInterpreterException("sscanf() second argument must be a format string");

        // Collect the variable identifiers (not evaluated)
        var varArgs = new List<Expression>();
        for (int i = 2; i < expr.Arguments.Count; i++)
        {
            varArgs.Add(expr.Arguments[i]);
        }

        // Parse the format string and extract values
        var values = ParseSscanfFormat(inputStr, formatStr);
        int assigned = 0;

        // Assign values to variables
        for (int i = 0; i < values.Count && i < varArgs.Count; i++)
        {
            if (values[i] == null) continue; // Skip if this was a %* format

            var varExpr = varArgs[i];
            if (varExpr is Identifier id)
            {
                // Simple variable assignment
                AssignToVariable(id.Name, values[i]!);
                assigned++;
            }
            else if (varExpr is IndexExpression indexExpr)
            {
                // Array/mapping element assignment
                SetIndexValue(indexExpr, values[i]!);
                assigned++;
            }
            else
            {
                throw new ObjectInterpreterException("sscanf() variable arguments must be identifiers or indexable expressions");
            }
        }

        return assigned;
    }

    /// <summary>
    /// Parse a string using sscanf format specifiers.
    /// Returns a list of parsed values (null for skipped %* patterns).
    /// </summary>
    private List<object?> ParseSscanfFormat(string input, string format)
    {
        var results = new List<object?>();
        int inputPos = 0;
        int formatPos = 0;

        while (formatPos < format.Length && inputPos <= input.Length)
        {
            if (format[formatPos] == '%' && formatPos + 1 < format.Length)
            {
                formatPos++;
                bool skip = false;

                // Check for * (skip)
                if (format[formatPos] == '*')
                {
                    skip = true;
                    formatPos++;
                }

                if (formatPos >= format.Length) break;

                char specifier = format[formatPos];
                formatPos++;

                // Look ahead in format to find the delimiter (next literal or end)
                string? delimiter = null;
                int nextLiteral = formatPos;
                while (nextLiteral < format.Length)
                {
                    if (format[nextLiteral] == '%')
                    {
                        break;
                    }
                    delimiter = (delimiter ?? "") + format[nextLiteral];
                    nextLiteral++;
                }

                switch (specifier)
                {
                    case 's':
                        // Match string up to delimiter or end
                        int endPos;
                        if (!string.IsNullOrEmpty(delimiter))
                        {
                            endPos = input.IndexOf(delimiter, inputPos, StringComparison.Ordinal);
                            if (endPos == -1) endPos = input.Length;
                        }
                        else
                        {
                            // No delimiter - for %s followed by %d, we need to stop at first digit
                            // For now, take rest of string or until whitespace
                            endPos = inputPos;
                            while (endPos < input.Length && !char.IsWhiteSpace(input[endPos]))
                            {
                                endPos++;
                            }
                        }

                        var strValue = input[inputPos..endPos];
                        if (!skip)
                        {
                            results.Add(strValue);
                        }
                        inputPos = endPos;

                        // Skip the delimiter if present
                        if (!string.IsNullOrEmpty(delimiter) && input[inputPos..].StartsWith(delimiter))
                        {
                            inputPos += delimiter.Length;
                            formatPos = nextLiteral;
                        }
                        break;

                    case 'd':
                        // Skip leading whitespace for %d
                        while (inputPos < input.Length && char.IsWhiteSpace(input[inputPos]))
                        {
                            inputPos++;
                        }

                        // Match integer
                        int intStart = inputPos;
                        if (inputPos < input.Length && (input[inputPos] == '-' || input[inputPos] == '+'))
                        {
                            inputPos++;
                        }
                        while (inputPos < input.Length && char.IsDigit(input[inputPos]))
                        {
                            inputPos++;
                        }

                        if (intStart == inputPos)
                        {
                            // No digits found
                            return results;
                        }

                        var intStr = input[intStart..inputPos];
                        if (int.TryParse(intStr, out int intValue))
                        {
                            if (!skip)
                            {
                                results.Add(intValue);
                            }
                        }
                        else
                        {
                            return results;
                        }

                        // Skip delimiter if present
                        if (!string.IsNullOrEmpty(delimiter) && inputPos < input.Length &&
                            input[inputPos..].StartsWith(delimiter))
                        {
                            inputPos += delimiter.Length;
                            formatPos = nextLiteral;
                        }
                        break;

                    default:
                        throw new ObjectInterpreterException($"sscanf() unknown format specifier '%{specifier}'");
                }
            }
            else
            {
                // Literal character - must match
                if (inputPos >= input.Length || input[inputPos] != format[formatPos])
                {
                    // Mismatch - stop parsing
                    break;
                }
                inputPos++;
                formatPos++;
            }
        }

        return results;
    }

    /// <summary>
    /// Assign a value to a variable by name (handles locals and object variables).
    /// </summary>
    private void AssignToVariable(string name, object value)
    {
        // Try local scopes first
        foreach (var scope in _localScopes)
        {
            if (scope.ContainsKey(name))
            {
                scope[name] = value;
                return;
            }
        }

        // Try object variables
        if (_currentObject.Variables.ContainsKey(name))
        {
            _currentObject.Variables[name] = value;
            return;
        }

        // Variable doesn't exist - create in local scope if we have one
        if (_localScopes.Count > 0)
        {
            _localScopes.Peek()[name] = value;
        }
        else
        {
            // No local scope - this shouldn't normally happen
            throw new ObjectInterpreterException($"Cannot assign to undefined variable '{name}'");
        }
    }

    /// <summary>
    /// Set a value at an index (for array/mapping element assignment).
    /// </summary>
    private void SetIndexValue(IndexExpression indexExpr, object value)
    {
        var target = Evaluate(indexExpr.Target);
        var index = Evaluate(indexExpr.Index);

        if (target is List<object> list)
        {
            var idx = Convert.ToInt32(index);
            if (idx >= 0 && idx < list.Count)
            {
                list[idx] = value;
            }
            else
            {
                throw new ObjectInterpreterException($"Array index {idx} out of bounds");
            }
        }
        else if (target is Dictionary<object, object> dict)
        {
            dict[index] = value;
        }
        else
        {
            throw new ObjectInterpreterException("Cannot index into non-array/mapping type");
        }
    }

    private object? CallUserFunction(FunctionDefinition funcDef, List<object> args)
    {
        // Use the current object's program as the owning program
        // This is the legacy behavior, but CallUserFunctionWithProgram should be preferred
        return CallUserFunctionWithProgram(funcDef, args, _currentObject.Program);
    }

    private object? CallUserFunctionWithProgram(FunctionDefinition funcDef, List<object> args, LpcProgram? owningProgram)
    {
        // Check argument count
        if (args.Count != funcDef.Parameters.Count)
        {
            throw new ObjectInterpreterException(
                $"Function '{funcDef.Name}' expects {funcDef.Parameters.Count} arguments, got {args.Count}");
        }

        // Create local scope for function parameters
        var localScope = new Dictionary<string, object?>();
        for (int i = 0; i < funcDef.Parameters.Count; i++)
        {
            localScope[funcDef.Parameters[i]] = args[i];
        }

        // Push local scope onto stack
        _localScopes.Push(localScope);

        // Push the owning program onto the executing programs stack
        // This is used for correct :: (parent call) resolution
        if (owningProgram != null)
        {
            _executingPrograms.Push(owningProgram);
        }

        // Track file/function for error messages and stack traces
        var previousFile = _currentFile;
        var previousLine = _currentLine;
        var filePath = owningProgram?.FilePath ?? _currentObject.ObjectName;
        _currentFile = filePath;
        _currentLine = funcDef.Body.Line;
        _traceStack.Push((filePath, funcDef.Name, funcDef.Body.Line));

        // Check recursion depth limit
        CheckRecursionDepth();

        try
        {
            // Execute function body
            Execute(funcDef.Body);
            return 0L; // Default return value
        }
        catch (ReturnException ret)
        {
            return ret.Value ?? 0L; // Return 0 if null
        }
        finally
        {
            // Pop trace stack
            _traceStack.Pop();
            _currentFile = previousFile;
            _currentLine = previousLine;

            // Pop local scope
            _localScopes.Pop();

            // Pop executing program
            if (owningProgram != null)
            {
                _executingPrograms.Pop();
            }
        }
    }

    private object EvaluateBinaryOp(BinaryOp expr)
    {
        // Short-circuit evaluation for logical operators
        if (expr.Operator == BinaryOperator.LogicalAnd)
        {
            var left = Evaluate(expr.Left);
            if (!IsTrue(left)) return 0L;
            var right = Evaluate(expr.Right);
            return IsTrue(right) ? 1L : 0L;
        }

        if (expr.Operator == BinaryOperator.LogicalOr)
        {
            var left = Evaluate(expr.Left);
            if (IsTrue(left)) return 1L;
            var right = Evaluate(expr.Right);
            return IsTrue(right) ? 1L : 0L;
        }

        // Evaluate both operands
        var leftValue = Evaluate(expr.Left);
        var rightValue = Evaluate(expr.Right);

        // String concatenation for +
        if (expr.Operator == BinaryOperator.Add && leftValue is string leftStr)
        {
            return leftStr + ToStr(rightValue);
        }

        // Array concatenation for +
        if (expr.Operator == BinaryOperator.Add && leftValue is List<object> leftArr && rightValue is List<object> rightArr)
        {
            var result = new List<object>(leftArr);
            result.AddRange(rightArr);
            return result;
        }

        // String comparison
        if (leftValue is string ls && rightValue is string rs)
        {
            return expr.Operator switch
            {
                BinaryOperator.Equal => string.Equals(ls, rs) ? 1L : 0L,
                BinaryOperator.NotEqual => !string.Equals(ls, rs) ? 1L : 0L,
                _ => throw new ObjectInterpreterException($"Cannot apply operator {expr.Operator} to strings")
            };
        }

        // Mixed type equality comparisons (string vs int, etc.) return false
        // This matches authentic LPC behavior
        if (expr.Operator == BinaryOperator.Equal || expr.Operator == BinaryOperator.NotEqual)
        {
            bool sameType = (IsInteger(leftValue) && IsInteger(rightValue)) ||
                           (leftValue is string && rightValue is string);
            if (!sameType)
            {
                return expr.Operator == BinaryOperator.Equal ? 0L : 1L;
            }
        }

        // Integer operations
        var left_i = ToInt(leftValue);
        var right_i = ToInt(rightValue);

        return expr.Operator switch
        {
            BinaryOperator.Add => left_i + right_i,
            BinaryOperator.Subtract => left_i - right_i,
            BinaryOperator.Multiply => left_i * right_i,
            BinaryOperator.Divide => right_i != 0 ? left_i / right_i
                : throw new ObjectInterpreterException("Division by zero"),
            BinaryOperator.Modulo => right_i != 0 ? left_i % right_i
                : throw new ObjectInterpreterException("Modulo by zero"),
            BinaryOperator.Less => left_i < right_i ? 1L : 0L,
            BinaryOperator.LessEqual => left_i <= right_i ? 1L : 0L,
            BinaryOperator.Greater => left_i > right_i ? 1L : 0L,
            BinaryOperator.GreaterEqual => left_i >= right_i ? 1L : 0L,
            BinaryOperator.Equal => left_i == right_i ? 1L : 0L,
            BinaryOperator.NotEqual => left_i != right_i ? 1L : 0L,
            BinaryOperator.BitwiseAnd => left_i & right_i,
            BinaryOperator.BitwiseOr => left_i | right_i,
            BinaryOperator.BitwiseXor => left_i ^ right_i,
            BinaryOperator.LeftShift => left_i << (int)right_i,
            BinaryOperator.RightShift => left_i >> (int)right_i,
            _ => throw new ObjectInterpreterException($"Unknown binary operator: {expr.Operator}")
        };
    }

    private object EvaluateUnaryOp(UnaryOp expr)
    {
        return expr.Operator switch
        {
            UnaryOperator.Negate => -ToInt(Evaluate(expr.Operand)),
            UnaryOperator.LogicalNot => IsTrue(Evaluate(expr.Operand)) ? 0 : 1,
            UnaryOperator.BitwiseNot => ~ToInt(Evaluate(expr.Operand)),
            UnaryOperator.PreIncrement => EvaluatePreIncrement((Identifier)expr.Operand),
            UnaryOperator.PreDecrement => EvaluatePreDecrement((Identifier)expr.Operand),
            UnaryOperator.PostIncrement => EvaluatePostIncrement((Identifier)expr.Operand),
            UnaryOperator.PostDecrement => EvaluatePostDecrement((Identifier)expr.Operand),
            _ => throw new ObjectInterpreterException($"Unknown unary operator: {expr.Operator}")
        };
    }

    private object EvaluatePreIncrement(Identifier id)
    {
        // Check local scope first
        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(id.Name, out var localVal))
        {
            var value = ToInt(localVal ?? 0);
            var newValue = value + 1;
            _localScopes.Peek()[id.Name] = newValue;
            return newValue;
        }

        var current = _currentObject.GetVariable(id.Name);
        var val = ToInt(current ?? 0);
        var newVal = val + 1;
        _currentObject.SetVariable(id.Name, newVal);
        return newVal;
    }

    private object EvaluatePreDecrement(Identifier id)
    {
        // Check local scope first
        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(id.Name, out var localVal))
        {
            var value = ToInt(localVal ?? 0);
            var newValue = value - 1;
            _localScopes.Peek()[id.Name] = newValue;
            return newValue;
        }

        var current = _currentObject.GetVariable(id.Name);
        var val = ToInt(current ?? 0);
        var newVal = val - 1;
        _currentObject.SetVariable(id.Name, newVal);
        return newVal;
    }

    private object EvaluatePostIncrement(Identifier id)
    {
        // Check local scope first
        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(id.Name, out var localVal))
        {
            var val = ToInt(localVal ?? 0);
            _localScopes.Peek()[id.Name] = val + 1;
            return val; // Return old value
        }

        var current = _currentObject.GetVariable(id.Name);
        var value = ToInt(current ?? 0);
        _currentObject.SetVariable(id.Name, value + 1);
        return value; // Return old value
    }

    private object EvaluatePostDecrement(Identifier id)
    {
        // Check local scope first
        if (_localScopes.Count > 0 && _localScopes.Peek().TryGetValue(id.Name, out var localVal))
        {
            var val = ToInt(localVal ?? 0);
            _localScopes.Peek()[id.Name] = val - 1;
            return val; // Return old value
        }

        var current = _currentObject.GetVariable(id.Name);
        var value = ToInt(current ?? 0);
        _currentObject.SetVariable(id.Name, value - 1);
        return value; // Return old value
    }

    private object EvaluateTernaryOp(TernaryOp expr)
    {
        var condition = Evaluate(expr.Condition);
        return IsTrue(condition) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);
    }

    #endregion

    #region Helper Methods

    private bool IsTrue(object? value)
    {
        return value switch
        {
            long l => l != 0,
            int i => i != 0,
            string s => !string.IsNullOrEmpty(s),
            _ => value != null
        };
    }

    /// <summary>
    /// Check if a value is an integer type (int or long).
    /// </summary>
    private static bool IsInteger(object? value)
    {
        return value is int or long;
    }

    private long ToInt(object? value)
    {
        return value switch
        {
            long l => l,
            int i => i,
            string s => long.TryParse(s, out var result) ? result : 0,
            null => 0,
            _ => 0
        };
    }

    private string ToStr(object? value)
    {
        return value switch
        {
            string s => s,
            long l => l.ToString(),
            int i => i.ToString(),
            _ => value?.ToString() ?? ""
        };
    }

    /// <summary>
    /// Get the current object (for this_object() efun).
    /// </summary>
    public MudObject GetCurrentObject()
    {
        return _currentObject;
    }

    /// <summary>
    /// Get the previous caller object (for previous_object() efun).
    /// </summary>
    public MudObject? GetPreviousObject()
    {
        return _callStack.Count > 0 ? _callStack.Peek() : null;
    }

    #endregion

    #region Object Efuns

    /// <summary>
    /// clone_object(path) - Create a new clone of an object.
    /// Requires Wizard+ access level. Path access checked.
    /// Returns the clone object.
    /// </summary>
    private object CloneObjectEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("clone_object() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("clone_object() requires a string path argument");
        }

        // Permission check: require Wizard+ and path access
        RequireAccessLevel(AccessLevel.Wizard, "clone_object");
        RequirePathAccess(path, "clone_object", isWrite: false);

        try
        {
            var clone = _objectManager.CloneObject(path);
            return clone;
        }
        catch (Exception ex)
        {
            throw new EfunException($"clone_object(\"{path}\") failed: {ex.Message}");
        }
    }

    /// <summary>
    /// this_object() - Returns the current object being executed.
    /// </summary>
    private object ThisObjectEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("this_object() takes no arguments");
        }

        return _currentObject;
    }

    /// <summary>
    /// load_object(path) - Load or get a blueprint object.
    /// Requires Wizard+ access level. Path access checked.
    /// Returns the blueprint object.
    /// </summary>
    private object LoadObjectEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("load_object() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("load_object() requires a string path argument");
        }

        // Permission check: require Wizard+ and path access
        RequireAccessLevel(AccessLevel.Wizard, "load_object");
        RequirePathAccess(path, "load_object", isWrite: false);

        try
        {
            var blueprint = _objectManager.LoadObject(path);
            return blueprint;
        }
        catch (Exception ex)
        {
            throw new EfunException($"load_object(\"{path}\") failed: {ex.Message}");
        }
    }

    /// <summary>
    /// find_object(name) - Find an object by its full name.
    /// Returns the object if found, or 0 if not found (LPC convention).
    /// </summary>
    private object FindObjectEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("find_object() requires exactly 1 argument");
        }

        if (args[0] is not string name)
        {
            throw new EfunException("find_object() requires a string name argument");
        }

        var obj = _objectManager.FindObject(name);
        return obj ?? (object)0; // LPC convention: return 0 for "not found"
    }

    /// <summary>
    /// destruct(object) - Destroy an object and remove it from the game.
    /// Requires Wizard+ access level. Cannot destruct /secure/ objects without Admin.
    /// Returns 1 on success.
    /// </summary>
    private object DestructEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("destruct() requires exactly 1 argument");
        }

        if (args[0] is not MudObject obj)
        {
            throw new EfunException("destruct() requires an object argument");
        }

        // Permission check: require Wizard+ access level
        RequireAccessLevel(AccessLevel.Wizard, "destruct");

        // Check if trying to destruct a /secure/ object (admin only)
        var accessLevel = GetCurrentAccessLevel();
        if (accessLevel < AccessLevel.Admin && IsSecurePath(obj.FilePath))
        {
            throw new EfunException("Permission denied: cannot destruct /secure/ objects without Admin access");
        }

        try
        {
            _objectManager.DestructObject(obj);
            return 1;
        }
        catch (Exception ex)
        {
            throw new EfunException($"destruct() failed: {ex.Message}");
        }
    }

    /// <summary>
    /// call_other(object, "function_name", args...) - Call a function on another object.
    /// Traditional LPC way to invoke methods on objects.
    /// Returns the function's return value, or 0 if function not found.
    /// </summary>
    private object CallOtherEfun(List<object> args)
    {
        if (args.Count < 2)
        {
            throw new EfunException("call_other() requires at least 2 arguments (object, function_name)");
        }

        if (args[0] is not MudObject target)
        {
            // If it's an int 0, return 0 (calling on null object)
            if (args[0] is int i && i == 0)
            {
                return 0;
            }
            throw new EfunException("call_other() first argument must be an object");
        }

        if (args[1] is not string functionName)
        {
            throw new EfunException("call_other() second argument must be a string (function name)");
        }

        // Gather remaining arguments for the function call
        var funcArgs = new List<object>();
        for (int i = 2; i < args.Count; i++)
        {
            funcArgs.Add(args[i]);
        }

        // Find the function
        var func = target.FindFunction(functionName);
        if (func == null)
        {
            // LPC convention: return 0 if function not found
            return 0;
        }

        // Security check: private, protected, and static functions cannot be called via call_other
        if ((func.Visibility & (FunctionVisibility.Private | FunctionVisibility.Protected | FunctionVisibility.Static)) != 0)
        {
            // LPC convention: return 0 as if function doesn't exist (don't reveal existence)
            return 0;
        }

        try
        {
            // Call the function on the target object
            var result = CallFunctionOnObject(target, functionName, funcArgs);
            return result ?? 0;
        }
        catch (ReturnException ret)
        {
            return ret.Value ?? 0;
        }
        catch (Exception ex)
        {
            throw new EfunException($"call_other() failed: {ex.Message}");
        }
    }

    /// <summary>
    /// move_object(destination) or move_object(what, destination)
    /// Moves an object to a new environment and calls init() hooks.
    /// Single arg: moves this_object() (or this_player()) to destination
    /// Two args: moves first arg to second arg
    /// Returns 1 on success, 0 on failure.
    ///
    /// After a successful move, init() is called on:
    /// 1. The destination (with this_player() = moving object)
    /// 2. All other objects already in the destination (with this_player() = moving object)
    /// </summary>
    private object MoveObjectEfun(List<object> args)
    {
        MudObject? what;
        MudObject? destination;

        if (args.Count == 1)
        {
            // Single arg: move this_player() or this_object() to destination
            var context = ExecutionContext.Current;
            what = context?.PlayerObject ?? _currentObject;

            // Check for 0 (null destination) - could be int or long
            if ((args[0] is int i && i == 0) || (args[0] is long l && l == 0))
            {
                destination = null; // Moving to null (remove from environment)
            }
            else if (args[0] is not MudObject dest)
            {
                throw new EfunException("move_object() destination must be an object");
            }
            else
            {
                destination = dest;
            }
        }
        else if (args.Count == 2)
        {
            // Two args: move first to second
            if (args[0] is not MudObject obj)
            {
                throw new EfunException("move_object() first argument must be an object");
            }
            what = obj;

            // Check for 0 (null destination) - could be int or long
            if ((args[1] is int i && i == 0) || (args[1] is long l && l == 0))
            {
                destination = null; // Moving to null (remove from environment)
            }
            else if (args[1] is not MudObject dest)
            {
                throw new EfunException("move_object() destination must be an object");
            }
            else
            {
                destination = dest;
            }
        }
        else
        {
            throw new EfunException("move_object() requires 1 or 2 arguments");
        }

        if (what == null)
        {
            return 0L;
        }

        // Perform the move
        var success = what.MoveTo(destination);
        if (!success)
        {
            return 0L;
        }

        // Call init() hooks if we moved to a valid destination
        if (destination != null)
        {
            CallInitHooks(what, destination);
        }

        return 1L;
    }

    /// <summary>
    /// Call init() on destination and all other objects in the destination.
    /// this_player() is set to the object that moved during these calls.
    /// </summary>
    private void CallInitHooks(MudObject movedObject, MudObject destination)
    {
        // Set up execution context with movedObject as this_player()
        var context = ExecutionContext.Current;
        var previousPlayer = context?.PlayerObject;

        // Create a temporary context if none exists
        var tempContext = context == null;
        if (tempContext)
        {
            // For init() calls outside of a command context, we still want this_player() to work
            ExecutionContext.SetCurrentForInit(movedObject);
        }
        else if (context != null)
        {
            // Save and replace the player object for init() calls
            context.SetPlayerObjectForInit(movedObject);
        }

        try
        {
            // Call init() on the destination (e.g., room)
            CallInitIfExists(destination);

            // Call init() on all OTHER objects in the destination
            foreach (var other in destination.Contents)
            {
                if (other != movedObject && !other.IsDestructed)
                {
                    CallInitIfExists(other);
                }
            }
        }
        finally
        {
            // Restore previous player object
            if (tempContext)
            {
                ExecutionContext.ClearCurrentForInit();
            }
            else if (context != null && previousPlayer != null)
            {
                context.SetPlayerObjectForInit(previousPlayer);
            }
        }
    }

    /// <summary>
    /// Call init() on an object if it has that function defined.
    /// Silently does nothing if the function doesn't exist.
    /// </summary>
    private void CallInitIfExists(MudObject obj)
    {
        var initFunc = obj.FindFunction("init");
        if (initFunc == null)
        {
            return; // No init() function, nothing to do
        }

        try
        {
            CallFunctionOnObject(obj, "init", new List<object>());
        }
        catch (ReturnException)
        {
            // Normal return from init() - ignore
        }
        catch (Exception ex)
        {
            // Log but don't fail the move
            Logger.Warning($"init() in {obj.ObjectName} threw: {ex.Message}", LogCategory.Object);
        }
    }

    /// <summary>
    /// present(name) or present(name, where)
    /// Finds an object by name/ID in an environment.
    ///
    /// With 1 arg: searches this_player()'s environment and inventory
    /// With 2 args: searches the specified object's inventory
    ///
    /// Name can be:
    /// - Simple name: "sword" - finds first matching object
    /// - Indexed name: "sword 2" - finds the second sword
    ///
    /// Returns the object if found, 0 if not found.
    /// </summary>
    private object PresentEfun(List<object> args)
    {
        if (args.Count < 1 || args.Count > 2)
        {
            throw new EfunException("present() requires 1 or 2 arguments");
        }

        // If first arg is already an object, check if it's in the environment
        if (args[0] is MudObject targetObj)
        {
            MudObject? container = args.Count == 2 && args[1] is MudObject c ? c : GetSearchContainer();
            if (container == null) return 0L;

            // Check if targetObj is in the container's contents
            return container.Contents.Contains(targetObj) ? targetObj : 0L;
        }

        // First arg should be a string name
        if (args[0] is not string nameArg)
        {
            throw new EfunException("present() first argument must be a string or object");
        }

        // Parse the name - might be "sword" or "sword 2"
        var (name, index) = ParsePresentName(nameArg);

        // Determine where to search
        MudObject? where;
        if (args.Count == 2)
        {
            if (args[1] is not MudObject whereObj)
            {
                // Check for 0 (null object) - could be int or long
                if ((args[1] is int i && i == 0) || (args[1] is long l && l == 0))
                {
                    return 0L; // present(name, 0) returns 0
                }
                throw new EfunException("present() second argument must be an object");
            }
            where = whereObj;
        }
        else
        {
            where = GetSearchContainer();
        }

        if (where == null)
        {
            return 0L;
        }

        // Search through contents
        int matchCount = 0;
        foreach (var obj in where.Contents)
        {
            if (obj.IsDestructed) continue;

            if (ObjectMatchesName(obj, name))
            {
                matchCount++;
                if (matchCount == index)
                {
                    return obj;
                }
            }
        }

        return 0L; // Not found
    }

    /// <summary>
    /// Get the default container to search (player's environment or player itself).
    /// </summary>
    private MudObject? GetSearchContainer()
    {
        var context = ExecutionContext.Current;
        var player = context?.PlayerObject;
        return player?.Environment;
    }

    /// <summary>
    /// Parse a present() name argument like "sword" or "sword 2".
    /// Returns the name and index (1-based, defaults to 1).
    /// </summary>
    private (string name, int index) ParsePresentName(string nameArg)
    {
        // Check if the last part is a number
        var parts = nameArg.Trim().Split(' ');
        if (parts.Length >= 2 && int.TryParse(parts[^1], out int idx) && idx > 0)
        {
            // "sword 2" -> ("sword", 2)
            var name = string.Join(" ", parts[..^1]);
            return (name.ToLowerInvariant(), idx);
        }

        // "sword" -> ("sword", 1)
        return (nameArg.ToLowerInvariant(), 1);
    }

    /// <summary>
    /// Check if an object matches a given name by calling id(name).
    /// Returns true if the object's id() function returns non-zero.
    /// No fallback to short description - objects must explicitly define their IDs.
    /// </summary>
    private bool ObjectMatchesName(MudObject obj, string name)
    {
        var idFunc = obj.FindFunction("id");
        if (idFunc == null)
        {
            return false; // No id() function means object can't be found by name
        }

        try
        {
            var result = CallFunctionOnObject(obj, "id", new List<object> { name });
            // Check for non-zero result (could be int or long)
            return (result is int i && i != 0) || (result is long l && l != 0);
        }
        catch (ReturnException ret)
        {
            // Check for non-zero result (could be int or long)
            return (ret.Value is int i && i != 0) || (ret.Value is long l && l != 0);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// object_name(obj) - Returns the full object name including clone number.
    /// If no argument, returns name of this_object().
    /// Examples: "/std/weapon", "/obj/sword#5"
    /// </summary>
    private object ObjectNameEfun(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            target = _currentObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is MudObject obj)
            {
                target = obj;
            }
            else if (args[0] is int i && i == 0)
            {
                return 0; // object_name(0) returns 0
            }
            else
            {
                throw new EfunException("object_name() argument must be an object");
            }
        }
        else
        {
            throw new EfunException("object_name() takes 0 or 1 argument");
        }

        return target?.ObjectName ?? (object)0;
    }

    /// <summary>
    /// file_name(obj) - Returns the file path of an object (without clone number).
    /// If no argument, returns file path of this_object().
    /// Example: "/std/weapon" (same for blueprint and clones)
    /// </summary>
    private object FileNameEfun(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            target = _currentObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is MudObject obj)
            {
                target = obj;
            }
            else if (args[0] is int i && i == 0)
            {
                return 0; // file_name(0) returns 0
            }
            else
            {
                throw new EfunException("file_name() argument must be an object");
            }
        }
        else
        {
            throw new EfunException("file_name() takes 0 or 1 argument");
        }

        return target?.FilePath ?? (object)0;
    }

    /// <summary>
    /// previous_object(n) - Returns the object that called the current function.
    /// n=0 (default): immediate caller
    /// n=1: caller's caller, etc.
    /// Returns 0 if no such caller exists.
    /// </summary>
    private object PreviousObjectEfun(List<object> args)
    {
        int n = 0;

        if (args.Count == 0)
        {
            n = 0;
        }
        else if (args.Count == 1)
        {
            if (args[0] is not int idx)
            {
                throw new EfunException("previous_object() argument must be an integer");
            }
            n = idx;
        }
        else
        {
            throw new EfunException("previous_object() takes 0 or 1 argument");
        }

        if (n < 0)
        {
            return 0;
        }

        // _callStack has the callers, with the most recent at the top
        // Skip n entries to get the nth previous object
        if (n >= _callStack.Count)
        {
            return 0;
        }

        // Convert stack to array to access by index
        var callers = _callStack.ToArray();
        return callers[n];
    }

    #region Shadow Efuns

    /// <summary>
    /// shadow(ob) - Make this_object() shadow `ob`.
    /// Function calls to `ob` will be intercepted by this_object() first.
    /// Returns 1 on success, 0 on failure.
    /// Cannot shadow objects that already have shadows, or create circular shadows.
    /// </summary>
    private object ShadowEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("shadow() requires exactly 1 argument");
        }

        if (args[0] is not MudObject target)
        {
            if (args[0] is int i && i == 0)
            {
                return 0L; // shadow(0) just returns 0
            }
            throw new EfunException("shadow() argument must be an object");
        }

        // Cannot shadow self
        if (target == _currentObject)
        {
            return 0L;
        }

        // Cannot shadow if target already has a shadow
        if (target.ShadowedBy != null)
        {
            return 0L;
        }

        // Cannot shadow if this object is already shadowing something
        if (_currentObject.Shadowing != null)
        {
            return 0L;
        }

        // Cannot shadow if this object is being shadowed
        if (_currentObject.ShadowedBy != null)
        {
            return 0L;
        }

        // Check for query_prevent_shadow function on target
        var preventFunc = target.FindFunction("query_prevent_shadow");
        if (preventFunc != null)
        {
            var prevent = CallFunctionOnObject(target, "query_prevent_shadow", new List<object>());
            if (prevent is int pi && pi != 0)
            {
                return 0L; // Target prevents shadowing
            }
            if (prevent is long pl && pl != 0)
            {
                return 0L;
            }
        }

        // Set up shadow relationship
        target.ShadowedBy = _currentObject;
        _currentObject.Shadowing = target;

        Logger.Debug($"{_currentObject.ObjectName} is now shadowing {target.ObjectName}", LogCategory.Object);
        return 1L;
    }

    /// <summary>
    /// query_shadowing(ob) - Returns the object that is shadowing `ob`.
    /// Returns 0 if not shadowed.
    /// </summary>
    private object QueryShadowingEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("query_shadowing() requires exactly 1 argument");
        }

        if (args[0] is not MudObject target)
        {
            if (args[0] is int i && i == 0)
            {
                return 0;
            }
            throw new EfunException("query_shadowing() argument must be an object");
        }

        return target.ShadowedBy ?? (object)0;
    }

    /// <summary>
    /// unshadow(ob) - Remove shadow from `ob`.
    /// Can only be called from the shadowing object itself.
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object UnshadowEfun(List<object> args)
    {
        // If no argument, unshadow whatever this_object() is shadowing
        if (args.Count == 0)
        {
            if (_currentObject.Shadowing == null)
            {
                return 0L;
            }

            var target = _currentObject.Shadowing;
            target.ShadowedBy = null;
            _currentObject.Shadowing = null;

            Logger.Debug($"{_currentObject.ObjectName} stopped shadowing {target.ObjectName}", LogCategory.Object);
            return 1L;
        }

        if (args.Count != 1)
        {
            throw new EfunException("unshadow() takes 0 or 1 argument");
        }

        if (args[0] is not MudObject target2)
        {
            if (args[0] is int i && i == 0)
            {
                return 0L;
            }
            throw new EfunException("unshadow() argument must be an object");
        }

        // Only the shadow can unshadow
        if (_currentObject.Shadowing != target2)
        {
            return 0L;
        }

        target2.ShadowedBy = null;
        _currentObject.Shadowing = null;

        Logger.Debug($"{_currentObject.ObjectName} stopped shadowing {target2.ObjectName}", LogCategory.Object);
        return 1L;
    }

    #endregion

    #region Living/Interactive Efuns

    /// <summary>
    /// set_living(flag) - Mark this_object() as living (1) or not living (0).
    /// Living objects can receive heartbeats, be found by find_living(), etc.
    /// </summary>
    private object SetLivingEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("set_living() requires exactly 1 argument");
        }

        var flag = args[0] is int i ? i != 0 : false;
        _currentObject.IsLiving = flag;
        return flag ? 1L : 0L;
    }

    /// <summary>
    /// living(obj) - Test if an object is living.
    /// Returns 1 if living, 0 if not.
    /// </summary>
    private object LivingEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("living() requires exactly 1 argument");
        }

        if (args[0] is MudObject obj)
        {
            return obj.IsLiving ? 1L : 0L;
        }

        if (args[0] is int i && i == 0)
        {
            return 0; // living(0) returns 0
        }

        throw new EfunException("living() argument must be an object");
    }

    /// <summary>
    /// set_living_name(name) - Set the living name for this_object().
    /// The name is used for find_living() lookups.
    /// </summary>
    private object SetLivingNameEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("set_living_name() requires exactly 1 argument");
        }

        if (args[0] is not string name)
        {
            throw new EfunException("set_living_name() argument must be a string");
        }

        _objectManager.SetLivingName(_currentObject, name);
        return 1;
    }

    /// <summary>
    /// query_living_name(obj) - Get the living name of an object.
    /// Returns the name or 0 if not set.
    /// </summary>
    private object QueryLivingNameEfun(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            target = _currentObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is MudObject obj)
            {
                target = obj;
            }
            else if (args[0] is int i && i == 0)
            {
                return 0;
            }
            else
            {
                throw new EfunException("query_living_name() argument must be an object");
            }
        }
        else
        {
            throw new EfunException("query_living_name() takes 0 or 1 argument");
        }

        return target?.LivingName ?? (object)0;
    }

    /// <summary>
    /// interactive(obj) - Test if an object is an interactive player.
    /// Returns 1 if connected player, 0 if not.
    /// </summary>
    private object InteractiveEfun(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            target = _currentObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is MudObject obj)
            {
                target = obj;
            }
            else if (args[0] is int i && i == 0)
            {
                return 0;
            }
            else
            {
                throw new EfunException("interactive() argument must be an object");
            }
        }
        else
        {
            throw new EfunException("interactive() takes 0 or 1 argument");
        }

        return target?.IsInteractive == true ? 1L : 0L;
    }

    /// <summary>
    /// find_living(name) - Find a living object by its living name.
    /// Returns the object or 0 if not found.
    /// </summary>
    private object FindLivingEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("find_living() requires exactly 1 argument");
        }

        if (args[0] is not string name)
        {
            throw new EfunException("find_living() argument must be a string");
        }

        return _objectManager.FindLiving(name) ?? (object)0;
    }

    /// <summary>
    /// find_player(name) - Find an interactive player by name.
    /// Returns the player object or 0 if not found.
    /// </summary>
    private object FindPlayerEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("find_player() requires exactly 1 argument");
        }

        if (args[0] is not string name)
        {
            throw new EfunException("find_player() argument must be a string");
        }

        return _objectManager.FindPlayer(name) ?? (object)0;
    }

    /// <summary>
    /// users() - Get an array of all connected players.
    /// Returns an array of player objects.
    /// </summary>
    private object UsersEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("users() takes no arguments");
        }

        return _objectManager.GetUsers().Cast<object>().ToList();
    }

    /// <summary>
    /// linkdead_users() - Return an array of all linkdead player objects.
    /// These are players whose connection was lost but are still in the game.
    /// </summary>
    private object LinkdeadUsersEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("linkdead_users() takes no arguments");
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return new List<object>();
        }

        return gameLoop.GetLinkdeadSessions()
            .Where(s => s.PlayerObject != null && !s.PlayerObject.IsDestructed)
            .Select(s => (object)s.PlayerObject!)
            .ToList();
    }

    /// <summary>
    /// query_linkdead(object) - Check if an object is a linkdead player.
    /// Returns 1 if linkdead, 0 otherwise.
    /// </summary>
    private object QueryLinkdeadEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("query_linkdead() requires 1 argument (object)");
        }

        if (args[0] is not MudObject obj)
        {
            return 0L;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0L;
        }

        // Check if this object is in the linkdead sessions
        return gameLoop.GetLinkdeadSessions()
            .Any(s => s.PlayerObject == obj) ? 1L : 0L;
    }

    #endregion

    #region Heartbeat Efuns

    /// <summary>
    /// set_heart_beat(flag) - Enable or disable heartbeat for this_object().
    /// When enabled, the object's heart_beat() function is called periodically.
    /// Returns the previous heartbeat state.
    /// </summary>
    private object SetHeartBeatEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("set_heart_beat() requires exactly 1 argument");
        }

        var flag = Convert.ToInt32(args[0]);
        var obj = _currentObject;
        var wasEnabled = obj.HeartbeatEnabled;

        if (flag != 0)
        {
            // Enable heartbeat
            obj.HeartbeatEnabled = true;
            GameLoop.Instance?.RegisterHeartbeat(obj);
        }
        else
        {
            // Disable heartbeat
            obj.HeartbeatEnabled = false;
            GameLoop.Instance?.UnregisterHeartbeat(obj);
        }

        return wasEnabled ? 1L : 0L;
    }

    /// <summary>
    /// query_heart_beat(obj) - Check if an object has heartbeat enabled.
    /// Returns 1 if enabled, 0 if disabled.
    /// If no argument, checks this_object().
    /// </summary>
    private object QueryHeartBeatEfun(List<object> args)
    {
        MudObject obj;

        if (args.Count == 0)
        {
            obj = _currentObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is not MudObject mudObj)
            {
                throw new EfunException("query_heart_beat() argument must be an object");
            }
            obj = mudObj;
        }
        else
        {
            throw new EfunException("query_heart_beat() takes 0 or 1 argument");
        }

        return obj.HeartbeatEnabled ? 1L : 0L;
    }

    #endregion

    #region Callout Efuns

    /// <summary>
    /// call_out(func, delay, args...) - Schedule a delayed function call.
    /// Returns the callout ID which can be used with remove_call_out().
    /// </summary>
    private object CallOutEfun(List<object> args)
    {
        if (args.Count < 2)
        {
            throw new EfunException("call_out() requires at least 2 arguments: function name and delay");
        }

        if (args[0] is not string function)
        {
            throw new EfunException("call_out() first argument must be a function name string");
        }

        var delay = Convert.ToInt32(args[1]);
        if (delay < 0)
        {
            delay = 0;
        }

        // Collect any additional arguments to pass to the function
        var callArgs = new List<object>();
        for (int i = 2; i < args.Count; i++)
        {
            callArgs.Add(args[i]);
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            throw new EfunException("call_out() requires an active game loop");
        }

        return gameLoop.ScheduleCallout(_currentObject, function, callArgs, delay);
    }

    /// <summary>
    /// remove_call_out(func_or_id) - Cancel a pending callout.
    /// If given a string, removes the first callout for that function on this_object().
    /// If given an integer, removes the callout with that ID.
    /// Returns the time remaining until the callout would have fired, or -1 if not found.
    /// </summary>
    private object RemoveCallOutEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("remove_call_out() requires exactly 1 argument");
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return -1;
        }

        if (args[0] is string function)
        {
            return gameLoop.RemoveCalloutByFunction(_currentObject, function);
        }
        else if (args[0] is int calloutId)
        {
            return gameLoop.RemoveCalloutById(calloutId);
        }
        else
        {
            var id = Convert.ToInt32(args[0]);
            return gameLoop.RemoveCalloutById(id);
        }
    }

    /// <summary>
    /// find_call_out(func) - Find the time remaining until a callout fires.
    /// Returns the seconds until the callout fires, or -1 if not found.
    /// </summary>
    private object FindCallOutEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("find_call_out() requires exactly 1 argument");
        }

        if (args[0] is not string function)
        {
            throw new EfunException("find_call_out() argument must be a function name string");
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return -1;
        }

        return gameLoop.FindCallout(_currentObject, function);
    }

    #endregion

    #region Array Callback Efuns

    /// <summary>
    /// filter_array(arr, func) - Filter an array using a callback function.
    /// Returns a new array containing only elements where func(element) returns non-zero.
    /// func can be a string (function name in this_object) or an object#function pair.
    /// </summary>
    private object FilterArrayEfun(List<object> args)
    {
        if (args.Count < 2)
        {
            throw new EfunException("filter_array() requires at least 2 arguments");
        }

        if (args[0] is not List<object> arr)
        {
            throw new EfunException("filter_array() first argument must be an array");
        }

        var result = new List<object>();
        string funcName;
        MudObject? targetObj = null;

        // Parse the callback specification
        if (args[1] is string fn)
        {
            funcName = fn;
            targetObj = _currentObject;
        }
        else
        {
            throw new EfunException("filter_array() callback must be a function name string");
        }

        // Collect any extra arguments to pass to callback
        var extraArgs = new List<object>();
        for (int i = 2; i < args.Count; i++)
        {
            extraArgs.Add(args[i]);
        }

        // Filter each element
        foreach (var item in arr)
        {
            var callArgs = new List<object> { item };
            callArgs.AddRange(extraArgs);

            try
            {
                var callResult = CallFunctionOnObject(targetObj, funcName, callArgs);

                // Non-zero result means keep the element
                bool keep = false;
                if (callResult is int i)
                {
                    keep = i != 0;
                }
                else if (callResult != null)
                {
                    keep = true;
                }

                if (keep)
                {
                    result.Add(item);
                }
            }
            catch (ReturnException ex)
            {
                // Handle return value
                bool keep = false;
                if (ex.Value is int i)
                {
                    keep = i != 0;
                }
                else if (ex.Value != null)
                {
                    keep = true;
                }

                if (keep)
                {
                    result.Add(item);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// map_array(arr, func) - Transform an array using a callback function.
    /// Returns a new array where each element is the result of func(original_element).
    /// func can be a string (function name in this_object).
    /// </summary>
    private object MapArrayEfun(List<object> args)
    {
        if (args.Count < 2)
        {
            throw new EfunException("map_array() requires at least 2 arguments");
        }

        if (args[0] is not List<object> arr)
        {
            throw new EfunException("map_array() first argument must be an array");
        }

        var result = new List<object>();
        string funcName;
        MudObject? targetObj = null;

        // Parse the callback specification
        if (args[1] is string fn)
        {
            funcName = fn;
            targetObj = _currentObject;
        }
        else
        {
            throw new EfunException("map_array() callback must be a function name string");
        }

        // Collect any extra arguments to pass to callback
        var extraArgs = new List<object>();
        for (int i = 2; i < args.Count; i++)
        {
            extraArgs.Add(args[i]);
        }

        // Map each element
        foreach (var item in arr)
        {
            var callArgs = new List<object> { item };
            callArgs.AddRange(extraArgs);

            try
            {
                var callResult = CallFunctionOnObject(targetObj, funcName, callArgs);
                result.Add(callResult ?? 0);
            }
            catch (ReturnException ex)
            {
                result.Add(ex.Value ?? 0);
            }
        }

        return result;
    }

    #endregion

    #region File I/O Efuns

    /// <summary>
    /// Resolve a mudlib path to a full file system path.
    /// Ensures the path stays within the mudlib directory for security.
    /// </summary>
    private string ResolveMudlibPath(string path)
    {
        // Normalize: remove leading / and .c extension if present
        path = path.TrimStart('/');
        if (!path.EndsWith(".c") && !Path.HasExtension(path))
        {
            // Don't add extension for file operations
        }

        // Get full path
        var fullPath = Path.GetFullPath(Path.Combine(_objectManager.MudlibPath, path));

        // Security check: ensure path is within mudlib directory
        if (!fullPath.StartsWith(_objectManager.MudlibPath))
        {
            throw new EfunException("Security violation: path traversal attempt detected");
        }

        return fullPath;
    }

    /// <summary>
    /// read_file(path, [start], [lines]) - Read contents of a file.
    /// Requires Wizard+ access level and path access.
    /// start: line to start at (1-based, default 1)
    /// lines: number of lines to read (default all)
    /// Returns the file contents as a string, or 0 if file doesn't exist.
    /// </summary>
    private object ReadFileEfun(List<object> args)
    {
        if (args.Count < 1 || args.Count > 3)
        {
            throw new EfunException("read_file() requires 1 to 3 arguments");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("read_file() first argument must be a path string");
        }

        // Permission check: require Wizard+ and path access for reading
        RequireAccessLevel(AccessLevel.Wizard, "read_file");
        RequirePathAccess(path, "read_file", isWrite: false);

        int startLine = 1;
        int? numLines = null;

        if (args.Count >= 2)
        {
            startLine = Convert.ToInt32(args[1]);
            if (startLine < 1) startLine = 1;
        }

        if (args.Count >= 3)
        {
            numLines = Convert.ToInt32(args[2]);
            if (numLines < 0) numLines = null;
        }

        try
        {
            var fullPath = ResolveMudlibPath(path);

            if (!File.Exists(fullPath))
            {
                return 0;
            }

            var lines = File.ReadAllLines(fullPath);

            // Apply line range
            int skipLines = startLine - 1;
            IEnumerable<string> selectedLines = lines.Skip(skipLines);

            if (numLines.HasValue)
            {
                selectedLines = selectedLines.Take(numLines.Value);
            }

            return string.Join("\n", selectedLines);
        }
        catch (IOException)
        {
            return 0;
        }
    }

    /// <summary>
    /// write_file(path, text, [flag]) - Write text to a file.
    /// Requires Wizard+ access level and write path access.
    /// flag: 0 = overwrite (default), 1 = append
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object WriteFileEfun(List<object> args)
    {
        if (args.Count < 2 || args.Count > 3)
        {
            throw new EfunException("write_file() requires 2 or 3 arguments");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("write_file() first argument must be a path string");
        }

        if (args[1] is not string text)
        {
            throw new EfunException("write_file() second argument must be a string");
        }

        // Permission check: require Wizard+ and path access for writing
        RequireAccessLevel(AccessLevel.Wizard, "write_file");
        RequirePathAccess(path, "write_file", isWrite: true);

        int flag = 0;
        if (args.Count >= 3)
        {
            flag = Convert.ToInt32(args[2]);
        }

        try
        {
            var fullPath = ResolveMudlibPath(path);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (flag == 1)
            {
                // Append
                File.AppendAllText(fullPath, text);
            }
            else
            {
                // Overwrite
                File.WriteAllText(fullPath, text);
            }

            return 1;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    /// <summary>
    /// file_size(path) - Get the size of a file in bytes.
    /// Requires Wizard+ access level and read path access.
    /// Returns the file size, or -1 if file doesn't exist.
    /// Returns -2 if path is a directory.
    /// </summary>
    private object FileSizeEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("file_size() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("file_size() argument must be a path string");
        }

        // Permission check: require Wizard+ and path access for reading
        RequireAccessLevel(AccessLevel.Wizard, "file_size");
        RequirePathAccess(path, "file_size", isWrite: false);

        try
        {
            var fullPath = ResolveMudlibPath(path);

            if (Directory.Exists(fullPath))
            {
                return -2; // Is a directory
            }

            if (!File.Exists(fullPath))
            {
                return -1; // Doesn't exist
            }

            var info = new FileInfo(fullPath);
            return (int)info.Length;
        }
        catch (IOException)
        {
            return -1;
        }
    }

    /// <summary>
    /// rm(path) - Delete a file.
    /// Requires Wizard+ access level and write path access.
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object RmEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("rm() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("rm() argument must be a path string");
        }

        // Permission check: require Wizard+ and path access for writing
        RequireAccessLevel(AccessLevel.Wizard, "rm");
        RequirePathAccess(path, "rm", isWrite: true);

        try
        {
            var fullPath = ResolveMudlibPath(path);

            if (!File.Exists(fullPath))
            {
                return 0;
            }

            File.Delete(fullPath);
            return 1;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    /// <summary>
    /// get_dir(path) - Get directory listing.
    /// Requires Wizard+ access level and read path access.
    /// Returns an array of filenames in the directory.
    /// If path is a file, returns ({ filename }) if it exists, or ({ }) if not.
    /// </summary>
    private object GetDirEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("get_dir() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("get_dir() argument must be a path string");
        }

        // Permission check: require Wizard+ and path access for reading
        RequireAccessLevel(AccessLevel.Wizard, "get_dir");
        RequirePathAccess(path, "get_dir", isWrite: false);

        try
        {
            var fullPath = ResolveMudlibPath(path);

            if (File.Exists(fullPath))
            {
                // Return just the filename
                return new List<object> { Path.GetFileName(fullPath) };
            }

            if (!Directory.Exists(fullPath))
            {
                return new List<object>();
            }

            var entries = new List<object>();

            // Add directories (with trailing /)
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                entries.Add(Path.GetFileName(dir) + "/");
            }

            // Add files
            foreach (var file in Directory.GetFiles(fullPath))
            {
                entries.Add(Path.GetFileName(file));
            }

            entries.Sort();
            return entries;
        }
        catch (IOException)
        {
            return new List<object>();
        }
    }

    #endregion

    #region Access Level Efuns

    /// <summary>
    /// set_access_level(username, level) - Set a user's access level.
    /// Requires Admin access level.
    /// Level can be "player", "wizard", or "admin" (case insensitive).
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object SetAccessLevelEfun(List<object> args)
    {
        if (args.Count != 2)
        {
            throw new EfunException("set_access_level() requires exactly 2 arguments");
        }

        if (args[0] is not string username)
        {
            throw new EfunException("set_access_level() first argument must be a username string");
        }

        // Permission check: Admin only
        RequireAccessLevel(AccessLevel.Admin, "set_access_level");

        // Parse the level
        AccessLevel newLevel;
        if (args[1] is string levelStr)
        {
            newLevel = levelStr.ToLowerInvariant() switch
            {
                "player" => AccessLevel.Player,
                "wizard" => AccessLevel.Wizard,
                "admin" => AccessLevel.Admin,
                _ => throw new EfunException($"Invalid access level: {levelStr}. Use 'player', 'wizard', or 'admin'.")
            };
        }
        else if (args[1] is long levelNum)
        {
            newLevel = levelNum switch
            {
                1 => AccessLevel.Player,
                2 => AccessLevel.Wizard,
                3 => AccessLevel.Admin,
                _ => throw new EfunException($"Invalid access level: {levelNum}. Use 1 (player), 2 (wizard), or 3 (admin).")
            };
        }
        else
        {
            throw new EfunException("set_access_level() second argument must be a level string or number");
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        return gameLoop.AccountManager.SetAccessLevel(username, newLevel) ? 1 : 0;
    }

    /// <summary>
    /// query_access_level([username]) - Query a user's access level.
    /// Without argument, returns current player's level.
    /// With argument, requires Wizard+ to query other users.
    /// Returns access level as integer: 0=guest, 1=player, 2=wizard, 3=admin.
    /// </summary>
    private object QueryAccessLevelEfun(List<object> args)
    {
        if (args.Count > 1)
        {
            throw new EfunException("query_access_level() takes 0 or 1 argument");
        }

        if (args.Count == 0)
        {
            // Query self
            return (long)GetCurrentAccessLevel();
        }

        if (args[0] is not string username)
        {
            throw new EfunException("query_access_level() argument must be a username string");
        }

        // Querying another user requires Wizard+ access
        var currentUsername = GetCurrentUsername();
        if (!string.Equals(username, currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            RequireAccessLevel(AccessLevel.Wizard, "query_access_level (other user)");
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0L;
        }

        return (long)gameLoop.AccountManager.GetAccessLevel(username);
    }

    /// <summary>
    /// homedir([username]) - Get wizard home directory path.
    /// Without argument, returns current player's home directory.
    /// With argument, requires Wizard+ to query other users.
    /// Returns the path as a string, or 0 if user is not a wizard.
    /// </summary>
    private object HomedirEfun(List<object> args)
    {
        if (args.Count > 1)
        {
            throw new EfunException("homedir() takes 0 or 1 argument");
        }

        string? targetUsername;

        if (args.Count == 0)
        {
            targetUsername = GetCurrentUsername();
        }
        else if (args[0] is string username)
        {
            targetUsername = username;

            // Querying another user requires Wizard+ access
            var currentUsername = GetCurrentUsername();
            if (!string.Equals(username, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                RequireAccessLevel(AccessLevel.Wizard, "homedir (other user)");
            }
        }
        else
        {
            throw new EfunException("homedir() argument must be a username string");
        }

        if (string.IsNullOrEmpty(targetUsername))
        {
            return 0;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        // Check if user has wizard+ access
        var level = gameLoop.AccountManager.GetAccessLevel(targetUsername);
        if (level < AccessLevel.Wizard)
        {
            return 0; // Not a wizard, no home directory
        }

        return $"/wizards/{targetUsername.ToLowerInvariant()}";
    }

    /// <summary>
    /// shutdown() - Initiate graceful server shutdown.
    /// Requires Admin access level.
    /// Announces to all players, saves all data, then shuts down.
    /// </summary>
    private object ShutdownEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("shutdown() takes no arguments");
        }

        // Require Admin access
        RequireAccessLevel(AccessLevel.Admin, "shutdown");

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            throw new EfunException("shutdown() failed - no game loop");
        }

        // Log who initiated shutdown
        var context = ExecutionContext.Current;
        var initiator = context?.PlayerObject != null
            ? GetPlayerName(context.PlayerObject)
            : "unknown";
        Logger.Info($"Shutdown initiated by {initiator}", LogCategory.System);

        // Schedule shutdown on a separate thread to allow this command to complete
        Task.Run(() =>
        {
            // Small delay to let the command response be sent
            Thread.Sleep(500);

            // Trigger graceful shutdown (this will save all players and announce)
            gameLoop.GracefulShutdown();

            // Signal the main loop to stop
            Environment.Exit(0);
        });

        return 1;
    }

    /// <summary>
    /// throw(value) - Throw an error that can be caught by catch().
    /// If not caught, becomes a runtime error.
    /// value can be any type, but string is most common.
    /// </summary>
    private object ThrowEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("throw() requires exactly 1 argument");
        }

        throw new LpcThrowException(args[0] ?? "");
    }

    /// <summary>
    /// syslog(level, message) - Log a message to the system log.
    /// level: "debug", "info", "warning", "error"
    /// message: The message to log
    /// Returns: 1 on success, 0 on invalid level
    /// </summary>
    private object SyslogEfun(List<object> args)
    {
        if (args.Count < 1 || args.Count > 2)
        {
            throw new EfunException("syslog() requires 1 or 2 arguments: syslog(message) or syslog(level, message)");
        }

        string message;
        LogLevel level = LogLevel.Info;

        if (args.Count == 1)
        {
            // syslog(message) - default to Info level
            message = args[0]?.ToString() ?? "";
        }
        else
        {
            // syslog(level, message)
            var levelStr = args[0]?.ToString()?.ToLowerInvariant() ?? "info";
            message = args[1]?.ToString() ?? "";

            level = levelStr switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warning" or "warn" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Info
            };
        }

        // Include the calling object in the log message
        var prefix = $"[{_currentObject.ObjectName}]";
        Logger.Log(level, LogCategory.LPC, $"{prefix} {message}");

        return 1L;
    }

    /// <summary>
    /// Get the display name for a player object (helper for shutdown logging).
    /// </summary>
    private string GetPlayerName(MudObject player)
    {
        if (player.FindFunction("query_name") != null)
        {
            try
            {
                var result = CallFunctionOnObject(player, "query_name", new List<object>());
                if (result is string name && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch
            {
                // Fall through
            }
        }
        return player.ObjectName;
    }

    #endregion

    #region Alias Management Efuns

    /// <summary>
    /// Get the current player's session (for alias operations).
    /// </summary>
    private PlayerSession? GetCurrentSession()
    {
        var context = ExecutionContext.Current;
        if (context?.PlayerObject == null) return null;

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null) return null;

        return gameLoop.GetAllSessions()
            .FirstOrDefault(s => s.PlayerObject == context.PlayerObject);
    }

    /// <summary>
    /// query_aliases() - Get all aliases for the current player.
    /// Returns a mapping of alias -> command.
    /// </summary>
    private object QueryAliasesEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("query_aliases() takes no arguments");
        }

        var session = GetCurrentSession();
        if (session == null)
        {
            return new Dictionary<object, object>();
        }

        // Convert to LPC mapping format
        var result = new Dictionary<object, object>();
        foreach (var kvp in session.Aliases)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
    }

    /// <summary>
    /// query_alias(name) - Get a specific alias definition.
    /// Returns the command string or 0 if not found.
    /// </summary>
    private object QueryAliasEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("query_alias() requires exactly 1 argument");
        }

        if (args[0] is not string aliasName)
        {
            throw new EfunException("query_alias() argument must be an alias name string");
        }

        var session = GetCurrentSession();
        if (session == null)
        {
            return 0;
        }

        return session.Aliases.TryGetValue(aliasName, out var command) ? command : 0;
    }

    /// <summary>
    /// set_alias(name, command) - Set or update an alias.
    /// Returns 1 on success, 0 on failure.
    /// Security: Cannot alias protected commands (quit, alias, password, etc.)
    /// </summary>
    private object SetAliasEfun(List<object> args)
    {
        if (args.Count != 2)
        {
            throw new EfunException("set_alias() requires exactly 2 arguments");
        }

        if (args[0] is not string aliasName)
        {
            throw new EfunException("set_alias() first argument must be an alias name string");
        }

        if (args[1] is not string command)
        {
            throw new EfunException("set_alias() second argument must be a command string");
        }

        // Security: Cannot alias protected commands
        if (GameLoop.IsProtectedCommand(aliasName))
        {
            throw new EfunException($"Cannot create alias for protected command '{aliasName}'");
        }

        var session = GetCurrentSession();
        if (session?.AuthenticatedUsername == null)
        {
            return 0;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        // Update session and persist to account
        session.Aliases[aliasName.ToLowerInvariant()] = command;
        gameLoop.AccountManager.SetAlias(session.AuthenticatedUsername, aliasName, command);

        return 1;
    }

    /// <summary>
    /// remove_alias(name) - Remove an alias.
    /// Returns 1 on success, 0 if not found or on failure.
    /// </summary>
    private object RemoveAliasEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("remove_alias() requires exactly 1 argument");
        }

        if (args[0] is not string aliasName)
        {
            throw new EfunException("remove_alias() argument must be an alias name string");
        }

        var session = GetCurrentSession();
        if (session?.AuthenticatedUsername == null)
        {
            return 0;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        // Update session and persist to account
        var removed = session.Aliases.Remove(aliasName.ToLowerInvariant());
        if (removed)
        {
            gameLoop.AccountManager.RemoveAlias(session.AuthenticatedUsername, aliasName);
        }

        return removed ? 1 : 0;
    }

    /// <summary>
    /// reset_aliases() - Reset all aliases to defaults.
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object ResetAliasesEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("reset_aliases() takes no arguments");
        }

        var session = GetCurrentSession();
        if (session?.AuthenticatedUsername == null)
        {
            return 0;
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        // Reset in account and reload into session
        if (gameLoop.AccountManager.ResetAliases(session.AuthenticatedUsername))
        {
            session.Aliases = gameLoop.AccountManager.GetAliases(session.AuthenticatedUsername);
            return 1;
        }

        return 0;
    }

    #endregion

    #region Object Persistence Efuns

    /// <summary>
    /// save_object(path) - Save this_object()'s variables to a file.
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object SaveObjectEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("save_object() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("save_object() argument must be a path string");
        }

        try
        {
            // Add .o extension if not present
            if (!path.EndsWith(".o"))
            {
                path = path + ".o";
            }

            var fullPath = ResolveMudlibPath(path);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();

            // Write each variable
            foreach (var (name, value) in _currentObject.Variables)
            {
                // Skip null/default values
                if (value == null) continue;
                if (value is int i && i == 0) continue;

                sb.Append(name);
                sb.Append(' ');
                sb.AppendLine(SerializeLpcValue(value));
            }

            File.WriteAllText(fullPath, sb.ToString());
            return 1;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// restore_object(path) - Restore this_object()'s variables from a file.
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object RestoreObjectEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("restore_object() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("restore_object() argument must be a path string");
        }

        try
        {
            // Add .o extension if not present
            if (!path.EndsWith(".o"))
            {
                path = path + ".o";
            }

            var fullPath = ResolveMudlibPath(path);

            if (!File.Exists(fullPath))
            {
                return 0;
            }

            var lines = File.ReadAllLines(fullPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Find the first space to separate name from value
                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex <= 0) continue;

                var name = line[..spaceIndex];
                var valueStr = line[(spaceIndex + 1)..];

                // Only restore if the variable exists in the object
                if (_currentObject.Variables.ContainsKey(name))
                {
                    var value = DeserializeLpcValue(valueStr);
                    _currentObject.Variables[name] = value;
                }
            }

            return 1;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Serialize an LPC value to string format.
    /// </summary>
    private string SerializeLpcValue(object? value)
    {
        if (value == null)
        {
            return "0";
        }

        return value switch
        {
            int i => i.ToString(),
            string s => "\"" + EscapeString(s) + "\"",
            List<object> arr => SerializeArray(arr),
            Dictionary<object, object> map => SerializeMapping(map),
            MudObject obj => obj.ObjectName, // Store object reference as path
            _ => value.ToString() ?? "0"
        };
    }

    private string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    private string SerializeArray(List<object> arr)
    {
        var sb = new StringBuilder();
        sb.Append("({");
        for (int i = 0; i < arr.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(SerializeLpcValue(arr[i]));
        }
        sb.Append("})");
        return sb.ToString();
    }

    private string SerializeMapping(Dictionary<object, object> map)
    {
        var sb = new StringBuilder();
        sb.Append("([");
        bool first = true;
        foreach (var (key, value) in map)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append(SerializeLpcValue(key));
            sb.Append(':');
            sb.Append(SerializeLpcValue(value));
        }
        sb.Append("])");
        return sb.ToString();
    }

    /// <summary>
    /// Deserialize an LPC value from string format.
    /// </summary>
    private object? DeserializeLpcValue(string str)
    {
        str = str.Trim();

        if (string.IsNullOrEmpty(str))
        {
            return 0;
        }

        // Integer
        if (int.TryParse(str, out int intVal))
        {
            return intVal;
        }

        // String
        if (str.StartsWith('"') && str.EndsWith('"'))
        {
            return UnescapeString(str[1..^1]);
        }

        // Array
        if (str.StartsWith("({") && str.EndsWith("})"))
        {
            return DeserializeArray(str[2..^2]);
        }

        // Mapping
        if (str.StartsWith("([") && str.EndsWith("])"))
        {
            return DeserializeMapping(str[2..^2]);
        }

        // Default: try to find as object, otherwise return as string
        return str;
    }

    private string UnescapeString(string s)
    {
        var result = new StringBuilder();
        bool escape = false;

        foreach (char c in s)
        {
            if (escape)
            {
                result.Append(c switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => c
                });
                escape = false;
            }
            else if (c == '\\')
            {
                escape = true;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private List<object> DeserializeArray(string content)
    {
        var result = new List<object>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        // Simple parsing - split by commas at depth 0
        var items = SplitAtDepthZero(content, ',');
        foreach (var item in items)
        {
            var value = DeserializeLpcValue(item.Trim());
            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private Dictionary<object, object> DeserializeMapping(string content)
    {
        var result = new Dictionary<object, object>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        // Split by commas at depth 0
        var pairs = SplitAtDepthZero(content, ',');
        foreach (var pair in pairs)
        {
            // Split key:value
            var colonIndex = FindAtDepthZero(pair, ':');
            if (colonIndex > 0)
            {
                var keyStr = pair[..colonIndex].Trim();
                var valueStr = pair[(colonIndex + 1)..].Trim();

                var key = DeserializeLpcValue(keyStr);
                var value = DeserializeLpcValue(valueStr);

                if (key != null)
                {
                    result[key] = value ?? 0;
                }
            }
        }

        return result;
    }

    private List<string> SplitAtDepthZero(string content, char delimiter)
    {
        var result = new List<string>();
        int depth = 0;
        bool inString = false;
        bool escape = false;
        int start = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '(' || c == '[' || c == '{')
            {
                depth++;
            }
            else if (c == ')' || c == ']' || c == '}')
            {
                depth--;
            }
            else if (c == delimiter && depth == 0)
            {
                result.Add(content[start..i]);
                start = i + 1;
            }
        }

        if (start < content.Length)
        {
            result.Add(content[start..]);
        }

        return result;
    }

    private int FindAtDepthZero(string content, char target)
    {
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '(' || c == '[' || c == '{')
            {
                depth++;
            }
            else if (c == ')' || c == ']' || c == '}')
            {
                depth--;
            }
            else if (c == target && depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Hot-Reload Efuns

    /// <summary>
    /// update(path) - Hot-reload an object and all objects that depend on it.
    /// Requires Wizard+ access level and path access.
    /// Returns the number of objects successfully updated.
    /// </summary>
    private object UpdateEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("update() requires exactly 1 argument");
        }

        if (args[0] is not string path)
        {
            throw new EfunException("update() argument must be a path string");
        }

        // Permission check: require Wizard+ and path access
        RequireAccessLevel(AccessLevel.Wizard, "update");
        RequirePathAccess(path, "update", isWrite: false);

        return _objectManager.UpdateObject(path);
    }

    /// <summary>
    /// inherits(path) - Get an array of paths that the given object inherits from.
    /// Returns an array of inherited object paths.
    /// </summary>
    private object InheritsEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("inherits() requires exactly 1 argument");
        }

        string path;
        if (args[0] is string s)
        {
            path = s;
        }
        else if (args[0] is MudObject obj)
        {
            path = obj.FilePath;
        }
        else
        {
            throw new EfunException("inherits() argument must be a path string or object");
        }

        return _objectManager.GetInheritanceParents(path).Cast<object>().ToList();
    }

    #endregion

    #region Input Handling Efuns

    /// <summary>
    /// input_to(func, [flags]) - Capture the next line of player input.
    /// The input will be passed to the specified function instead of being
    /// processed as a command.
    ///
    /// Flags:
    /// 0 = normal (default)
    /// 1 = no echo (hide input, for passwords)
    ///
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private object InputToEfun(List<object> args)
    {
        if (args.Count < 1 || args.Count > 2)
        {
            throw new EfunException("input_to() requires 1 or 2 arguments");
        }

        if (args[0] is not string function)
        {
            throw new EfunException("input_to() first argument must be a function name");
        }

        int flags = 0;
        if (args.Count >= 2)
        {
            flags = Convert.ToInt32(args[1]);
        }

        // Get the current execution context to find the player's session
        var context = ExecutionContext.Current;
        if (context == null)
        {
            return 0; // No execution context
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0; // No game loop
        }

        // Find the session for this connection
        var session = gameLoop.GetSession(context.ConnectionId);
        if (session == null)
        {
            return 0; // No session
        }

        // Set up the input handler
        session.PendingInputHandler = new InputHandler
        {
            Target = _currentObject,
            Function = function,
            Flags = flags
        };

        return 1;
    }

    #endregion

    #region Action System Efuns

    /// <summary>
    /// add_action(func, verb) or add_action(func, verb, flag)
    /// Register an action handler for a verb on this_object().
    /// The action is available to players who can see this object (in same room or inventory).
    /// flag: 0 = exact match (default), 1 = prefix match, 2 = override core commands
    /// Returns 1.
    /// </summary>
    private object AddActionEfun(List<object> args)
    {
        if (args.Count < 2 || args.Count > 3)
        {
            throw new EfunException("add_action() requires 2 or 3 arguments");
        }

        if (args[0] is not string function)
        {
            throw new EfunException("add_action() first argument must be a function name");
        }

        if (args[1] is not string verb)
        {
            throw new EfunException("add_action() second argument must be a verb string");
        }

        var flags = MudObject.ActionFlags.None;
        if (args.Count == 3)
        {
            var flagValue = ToInt(args[2]);
            if ((flagValue & 1) != 0)
            {
                flags |= MudObject.ActionFlags.MatchPrefix;
            }
            if ((flagValue & 2) != 0)
            {
                flags |= MudObject.ActionFlags.OverrideCore;
            }
        }

        _currentObject.AddAction(function, verb, flags);
        return 1;
    }

    /// <summary>
    /// query_verb() - Returns the current command verb being processed.
    /// Returns 0 if not in a command context.
    /// </summary>
    private object QueryVerbEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("query_verb() takes no arguments");
        }

        var context = ExecutionContext.Current;
        if (context?.CurrentVerb != null)
        {
            return context.CurrentVerb;
        }
        return 0;
    }

    /// <summary>
    /// notify_fail(msg) - Set the failure message for the current command.
    /// If no action handler returns 1, this message is shown instead of "What?".
    /// Returns 1.
    /// </summary>
    private object NotifyFailEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("notify_fail() requires exactly 1 argument");
        }

        if (args[0] is not string message)
        {
            throw new EfunException("notify_fail() argument must be a string");
        }

        var context = ExecutionContext.Current;
        if (context != null)
        {
            context.NotifyFailMessage = message;
        }
        return 1;
    }

    /// <summary>
    /// enable_commands() - Enable command processing for this object.
    /// Typically called by player objects to indicate they can receive commands.
    /// Returns 1.
    /// </summary>
    private object EnableCommandsEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("enable_commands() takes no arguments");
        }

        _currentObject.CommandsEnabled = true;
        return 1;
    }

    /// <summary>
    /// disable_commands() - Disable command processing for this object.
    /// Returns 1.
    /// </summary>
    private object DisableCommandsEfun(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("disable_commands() takes no arguments");
        }

        _currentObject.CommandsEnabled = false;
        return 1;
    }

    /// <summary>
    /// command(str) - Execute a command as this_player().
    /// Useful for forcing actions or implementing aliases.
    /// Returns 1 if the command was handled, 0 otherwise.
    /// </summary>
    private object CommandEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("command() requires exactly 1 argument");
        }

        if (args[0] is not string cmdString)
        {
            throw new EfunException("command() argument must be a string");
        }

        var context = ExecutionContext.Current;
        if (context?.PlayerObject == null)
        {
            return 0; // No player context
        }

        var gameLoop = GameLoop.Instance;
        if (gameLoop == null)
        {
            return 0;
        }

        // Queue the command for processing
        // Note: We execute immediately in the current context rather than queueing
        // to maintain LPC semantics where command() is synchronous
        return gameLoop.ExecuteCommandImmediate(context, cmdString) ? 1L : 0L;
    }

    #endregion

    #region Additional Object Efuns

    /// <summary>
    /// clonep(obj) - Returns 1 if obj is a clone (not a blueprint), 0 otherwise.
    /// </summary>
    private object ClonepEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("clonep() requires exactly 1 argument");
        }

        if (args[0] is not MudObject obj)
        {
            return 0; // Not an object, so not a clone
        }

        return obj.IsBlueprint ? 0 : 1;
    }

    /// <summary>
    /// say(msg) - Send message to all objects in the same room as this_player(),
    /// except this_player() themselves. Convenience wrapper for tell_room().
    /// </summary>
    private object SayEfun(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("say() requires exactly 1 argument");
        }

        if (args[0] is not string message)
        {
            throw new EfunException("say() argument must be a string");
        }

        var context = ExecutionContext.Current;
        var player = context?.PlayerObject;

        if (player == null)
        {
            return 0; // No player context
        }

        var room = player.Environment;
        if (room == null)
        {
            return 0; // Player not in a room
        }

        // Send to all objects in room except player
        foreach (var obj in room.Contents)
        {
            if (obj == player || obj.IsDestructed)
            {
                continue;
            }

            // If the object is interactive (a player), send the message
            if (obj.IsInteractive && !string.IsNullOrEmpty(obj.ConnectionId))
            {
                context?.OutputQueue?.Enqueue(new OutputMessage
                {
                    ConnectionId = obj.ConnectionId,
                    Content = message
                });
            }

            // Also try catch_tell() for NPCs
            var catchTell = obj.FindFunction("catch_tell");
            if (catchTell != null)
            {
                try
                {
                    CallFunctionOnObject(obj, "catch_tell", new List<object> { message });
                }
                catch (ReturnException)
                {
                    // Normal return
                }
                catch
                {
                    // Ignore errors in catch_tell
                }
            }
        }

        return 1;
    }

    #endregion

    #endregion
}

/// <summary>
/// Exception thrown during object execution.
/// </summary>
public class ObjectInterpreterException : Exception
{
    public ObjectInterpreterException(string message) : base(message) { }
    public ObjectInterpreterException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// LPC runtime error with file, line, and stack trace information.
/// </summary>
public class LpcRuntimeException : Exception
{
    public string File { get; }
    public int Line { get; }
    public string LpcStackTrace { get; }

    public LpcRuntimeException(string message, string file, int line, string lpcStackTrace = "")
        : base(FormatMessage(message, file, line, lpcStackTrace))
    {
        File = file;
        Line = line;
        LpcStackTrace = lpcStackTrace;
    }

    private static string FormatMessage(string message, string file, int line, string lpcStackTrace)
    {
        var result = $"{file}:{line}: {message}";
        if (!string.IsNullOrEmpty(lpcStackTrace))
        {
            result += "\n" + lpcStackTrace;
        }
        return result;
    }
}

/// <summary>
/// Exception thrown when execution limits are exceeded.
/// This prevents infinite loops and infinite recursion from crashing the game.
/// </summary>
public class ExecutionLimitException : Exception
{
    public string File { get; }
    public int Line { get; }

    public ExecutionLimitException(string message) : base(message)
    {
        File = "";
        Line = 0;
    }

    public ExecutionLimitException(string message, string file, int line)
        : base($"{file}:{line}: {message}")
    {
        File = file;
        Line = line;
    }
}

/// <summary>
/// Exception thrown by the LPC throw() efun.
/// Can be caught by catch() expression.
/// </summary>
public class LpcThrowException : Exception
{
    public object ThrownValue { get; }

    public LpcThrowException(object value) : base(FormatMessage(value))
    {
        ThrownValue = value;
    }

    private static string FormatMessage(object value)
    {
        return value switch
        {
            string s => s,
            _ => value?.ToString() ?? "*throw with no message*"
        };
    }
}
