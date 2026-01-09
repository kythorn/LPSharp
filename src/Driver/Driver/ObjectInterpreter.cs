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
        var func = target.FindFunction(functionName);
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
            return CallUserFunction(func, args);
        }
        finally
        {
            _currentObject = previousObject;
            _callStack.Pop();
        }
    }

    #region Statement Execution

    private object? Execute(Statement stmt)
    {
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
        // Variable already exists in object (created during object initialization)
        // Just set the initializer value if provided
        if (varDecl.Initializer != null)
        {
            var value = Evaluate(varDecl.Initializer);
            _currentObject.SetVariable(varDecl.Name, value);
        }
        return null;
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
        return expr switch
        {
            NumberLiteral num => num.Value,
            StringLiteral str => str.Value,
            Identifier id => EvaluateIdentifier(id),
            BinaryOp bin => EvaluateBinaryOp(bin),
            UnaryOp unary => EvaluateUnaryOp(unary),
            GroupedExpression grouped => Evaluate(grouped.Inner),
            TernaryOp ternary => EvaluateTernaryOp(ternary),
            Assignment assign => EvaluateAssignment(assign),
            CompoundAssignment compound => EvaluateCompoundAssignment(compound),
            FunctionCall call => EvaluateFunctionCall(call),
            _ => throw new ObjectInterpreterException($"Unknown expression type: {expr.GetType().Name}")
        };
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
            var parentFunc = _currentObject.FindParentFunction(expr.Name);
            if (parentFunc == null)
            {
                throw new ObjectInterpreterException(
                    $"Parent function '{expr.Name}' not found in inheritance chain of {_currentObject.ObjectName}");
            }

            return CallUserFunction(parentFunc, args) ?? 0;
        }

        // Check in current object's program (including inherited functions)
        var objectFunc = _currentObject.FindFunction(expr.Name);
        if (objectFunc != null)
        {
            return CallUserFunction(objectFunc, args) ?? 0;
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
