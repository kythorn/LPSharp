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
                "This usually indicates an infinite loop.");
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
    /// </summary>
    public object? CallFunctionOnObject(MudObject target, string functionName, List<object> args)
    {
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
        // Count each statement execution for limit checking
        CountInstruction();

        return stmt switch
        {
            BlockStatement block => ExecuteBlock(block),
            ExpressionStatement exprStmt => Evaluate(exprStmt.Expression),
            IfStatement ifStmt => ExecuteIf(ifStmt),
            WhileStatement whileStmt => ExecuteWhile(whileStmt),
            ForStatement forStmt => ExecuteFor(forStmt),
            ReturnStatement ret => ExecuteReturn(ret),
            BreakStatement => throw new BreakException(),
            ContinueStatement => throw new ContinueException(),
            VariableDeclaration varDecl => ExecuteVariableDeclaration(varDecl),
            _ => throw new ObjectInterpreterException($"Unknown statement type: {stmt.GetType().Name}")
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
            _ => 0  // int, object, mixed, void, etc. default to 0
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
            IndexExpression idx => EvaluateIndexExpression(idx),
            _ => throw new ObjectInterpreterException($"Unknown expression type: {expr.GetType().Name}")
        };
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
            return 0; // LPC returns 0 for missing mapping keys
        }

        throw new ObjectInterpreterException($"Cannot index into {target?.GetType().Name ?? "null"}");
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
            BinaryOperator.LeftShift => left_i << right_i,
            BinaryOperator.RightShift => left_i >> right_i,
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
                throw new ObjectInterpreterException(
                    $"Parent function '{expr.Name}' not found in inheritance chain");
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
                throw new ObjectInterpreterException(ex.Message);
            }
        }

        throw new ObjectInterpreterException($"Unknown function '{expr.Name}' in {_currentObject.ObjectName}");
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

        // Check recursion depth limit
        CheckRecursionDepth();

        try
        {
            // Execute function body
            Execute(funcDef.Body);
            return 0; // Default return value
        }
        catch (ReturnException ret)
        {
            return ret.Value ?? 0; // Return 0 if null
        }
        finally
        {
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
            if (!IsTrue(left)) return 0;
            var right = Evaluate(expr.Right);
            return IsTrue(right) ? 1 : 0;
        }

        if (expr.Operator == BinaryOperator.LogicalOr)
        {
            var left = Evaluate(expr.Left);
            if (IsTrue(left)) return 1;
            var right = Evaluate(expr.Right);
            return IsTrue(right) ? 1 : 0;
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
                BinaryOperator.Equal => string.Equals(ls, rs) ? 1 : 0,
                BinaryOperator.NotEqual => !string.Equals(ls, rs) ? 1 : 0,
                _ => throw new ObjectInterpreterException($"Cannot apply operator {expr.Operator} to strings")
            };
        }

        // Mixed type equality comparisons (string vs int, etc.) return false
        // This matches authentic LPC behavior
        if (expr.Operator == BinaryOperator.Equal || expr.Operator == BinaryOperator.NotEqual)
        {
            bool sameType = (leftValue is int && rightValue is int) ||
                           (leftValue is string && rightValue is string);
            if (!sameType)
            {
                return expr.Operator == BinaryOperator.Equal ? 0 : 1;
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
            BinaryOperator.Less => left_i < right_i ? 1 : 0,
            BinaryOperator.LessEqual => left_i <= right_i ? 1 : 0,
            BinaryOperator.Greater => left_i > right_i ? 1 : 0,
            BinaryOperator.GreaterEqual => left_i >= right_i ? 1 : 0,
            BinaryOperator.Equal => left_i == right_i ? 1 : 0,
            BinaryOperator.NotEqual => left_i != right_i ? 1 : 0,
            BinaryOperator.BitwiseAnd => left_i & right_i,
            BinaryOperator.BitwiseOr => left_i | right_i,
            BinaryOperator.BitwiseXor => left_i ^ right_i,
            BinaryOperator.LeftShift => left_i << right_i,
            BinaryOperator.RightShift => left_i >> right_i,
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
            int i => i != 0,
            string s => !string.IsNullOrEmpty(s),
            _ => value != null
        };
    }

    private int ToInt(object? value)
    {
        return value switch
        {
            int i => i,
            string s => int.TryParse(s, out var result) ? result : 0,
            null => 0,
            _ => 0
        };
    }

    private string ToStr(object? value)
    {
        return value switch
        {
            string s => s,
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

            if (args[0] is int i && i == 0)
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

            if (args[1] is int i && i == 0)
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
            return 0;
        }

        // Perform the move
        var success = what.MoveTo(destination);
        if (!success)
        {
            return 0;
        }

        // Call init() hooks if we moved to a valid destination
        if (destination != null)
        {
            CallInitHooks(what, destination);
        }

        return 1;
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
            Console.WriteLine($"Warning: init() in {obj.ObjectName} threw: {ex.Message}");
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
            if (container == null) return 0;

            // Check if targetObj is in the container's contents
            return container.Contents.Contains(targetObj) ? targetObj : 0;
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
                if (args[1] is int i && i == 0)
                {
                    return 0; // present(name, 0) returns 0
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
            return 0;
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

        return 0; // Not found
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
            return result is int i && i != 0;
        }
        catch (ReturnException ret)
        {
            return ret.Value is int i && i != 0;
        }
        catch
        {
            return false;
        }
    }

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
/// Exception thrown when execution limits are exceeded.
/// This prevents infinite loops and infinite recursion from crashing the game.
/// </summary>
public class ExecutionLimitException : Exception
{
    public ExecutionLimitException(string message) : base(message) { }
}
