namespace Driver;

/// <summary>
/// Tree-walking interpreter for LPC expressions and statements.
/// Evaluates AST nodes and returns results.
/// </summary>
public class Interpreter
{
    private readonly Dictionary<string, object> _variables = new();
    private readonly EfunRegistry _efuns;

    public Interpreter(TextWriter? output = null)
    {
        _efuns = new EfunRegistry(output);
    }

    #region Statement Execution

    /// <summary>
    /// Execute a statement. Returns the last expression value if any.
    /// </summary>
    public object? Execute(Statement stmt)
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
            _ => throw new InterpreterException($"Unknown statement type: {stmt.GetType().Name}", stmt)
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
        object? value = null;
        if (stmt.Value != null)
        {
            value = Evaluate(stmt.Value);
        }
        throw new ReturnException(value);
    }

    #endregion

    #region Expression Evaluation

    /// <summary>
    /// Evaluate an expression and return the result.
    /// Returns int for numeric results, string for string results.
    /// </summary>
    public object Evaluate(Expression expr)
    {
        return expr switch
        {
            NumberLiteral n => n.Value,
            StringLiteral s => s.Value,
            GroupedExpression g => Evaluate(g.Inner),
            UnaryOp u => EvaluateUnary(u),
            BinaryOp b => EvaluateBinary(b),
            TernaryOp t => EvaluateTernary(t),
            Identifier id => EvaluateIdentifier(id),
            Assignment a => EvaluateAssignment(a),
            CompoundAssignment ca => EvaluateCompoundAssignment(ca),
            FunctionCall fc => EvaluateFunctionCall(fc),
            _ => throw new InterpreterException($"Unknown expression type: {expr.GetType().Name}", expr)
        };
    }

    private object EvaluateIdentifier(Identifier expr)
    {
        if (_variables.TryGetValue(expr.Name, out var value))
        {
            return value;
        }
        throw new InterpreterException($"Undefined variable '{expr.Name}'", expr);
    }

    private object EvaluateAssignment(Assignment expr)
    {
        var value = Evaluate(expr.Value);
        _variables[expr.Name] = value;
        return value;
    }

    private object EvaluateCompoundAssignment(CompoundAssignment expr)
    {
        // Get current value (must exist)
        if (!_variables.TryGetValue(expr.Name, out var currentValue))
        {
            throw new InterpreterException($"Undefined variable '{expr.Name}'", expr);
        }

        // Evaluate the right-hand side
        var rightValue = Evaluate(expr.Value);

        // Handle string concatenation for +=
        if (expr.Operator == BinaryOperator.Add && (currentValue is string || rightValue is string))
        {
            var result = ToString(currentValue) + ToString(rightValue);
            _variables[expr.Name] = result;
            return result;
        }

        // Integer operations
        var left_i = ToInt(currentValue, expr);
        var right_i = ToInt(rightValue, expr);

        var newValue = expr.Operator switch
        {
            BinaryOperator.Add => left_i + right_i,
            BinaryOperator.Subtract => left_i - right_i,
            BinaryOperator.Multiply => left_i * right_i,
            BinaryOperator.Divide => right_i != 0
                ? left_i / right_i
                : throw new InterpreterException("Division by zero", expr),
            BinaryOperator.Modulo => right_i != 0
                ? left_i % right_i
                : throw new InterpreterException("Modulo by zero", expr),
            BinaryOperator.BitwiseAnd => left_i & right_i,
            BinaryOperator.BitwiseOr => left_i | right_i,
            BinaryOperator.BitwiseXor => left_i ^ right_i,
            BinaryOperator.LeftShift => left_i << right_i,
            BinaryOperator.RightShift => left_i >> right_i,
            _ => throw new InterpreterException($"Unsupported compound assignment operator: {expr.Operator}", expr)
        };

        _variables[expr.Name] = newValue;
        return newValue;
    }

    private object EvaluateFunctionCall(FunctionCall expr)
    {
        // Look up the efun
        if (!_efuns.TryGet(expr.Name, out var efun) || efun == null)
        {
            throw new InterpreterException($"Unknown function '{expr.Name}'", expr);
        }

        // Evaluate all arguments
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToList();

        // Call the efun
        try
        {
            return efun(args);
        }
        catch (EfunException ex)
        {
            throw new InterpreterException(ex.Message, expr);
        }
    }

    private object EvaluateUnary(UnaryOp expr)
    {
        // Handle increment/decrement specially since they modify variables
        if (expr.Operator is UnaryOperator.PreIncrement or UnaryOperator.PreDecrement
            or UnaryOperator.PostIncrement or UnaryOperator.PostDecrement)
        {
            return EvaluateIncrementDecrement(expr);
        }

        var operand = Evaluate(expr.Operand);

        return expr.Operator switch
        {
            UnaryOperator.Negate => -(ToInt(operand, expr)),
            UnaryOperator.LogicalNot => IsTrue(operand) ? 0 : 1,
            UnaryOperator.BitwiseNot => ~ToInt(operand, expr),
            _ => throw new InterpreterException($"Unknown unary operator: {expr.Operator}", expr)
        };
    }

    private object EvaluateIncrementDecrement(UnaryOp expr)
    {
        // Operand must be an identifier
        if (expr.Operand is not Identifier id)
        {
            throw new InterpreterException("Increment/decrement requires a variable", expr);
        }

        // Get current value
        if (!_variables.TryGetValue(id.Name, out var currentValue))
        {
            throw new InterpreterException($"Undefined variable '{id.Name}'", expr);
        }

        var currentInt = ToInt(currentValue, expr);
        var newValue = expr.Operator switch
        {
            UnaryOperator.PreIncrement or UnaryOperator.PostIncrement => currentInt + 1,
            UnaryOperator.PreDecrement or UnaryOperator.PostDecrement => currentInt - 1,
            _ => throw new InterpreterException($"Unexpected operator: {expr.Operator}", expr)
        };

        // Store the new value
        _variables[id.Name] = newValue;

        // Return based on prefix vs postfix
        return expr.Operator switch
        {
            UnaryOperator.PreIncrement or UnaryOperator.PreDecrement => newValue,
            UnaryOperator.PostIncrement or UnaryOperator.PostDecrement => currentInt,
            _ => throw new InterpreterException($"Unexpected operator: {expr.Operator}", expr)
        };
    }

    private object EvaluateBinary(BinaryOp expr)
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

        // For all other operators, evaluate both sides
        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);

        // String concatenation
        if (expr.Operator == BinaryOperator.Add && (leftVal is string || rightVal is string))
        {
            return ToString(leftVal) + ToString(rightVal);
        }

        // String equality
        if (leftVal is string leftStr && rightVal is string rightStr)
        {
            return expr.Operator switch
            {
                BinaryOperator.Equal => leftStr == rightStr ? 1 : 0,
                BinaryOperator.NotEqual => leftStr != rightStr ? 1 : 0,
                _ => throw new InterpreterException($"Operator {expr.Operator} not supported for strings", expr)
            };
        }

        // Integer operations
        var left_i = ToInt(leftVal, expr);
        var right_i = ToInt(rightVal, expr);

        return expr.Operator switch
        {
            // Arithmetic
            BinaryOperator.Add => left_i + right_i,
            BinaryOperator.Subtract => left_i - right_i,
            BinaryOperator.Multiply => left_i * right_i,
            BinaryOperator.Divide => right_i != 0
                ? left_i / right_i
                : throw new InterpreterException("Division by zero", expr),
            BinaryOperator.Modulo => right_i != 0
                ? left_i % right_i
                : throw new InterpreterException("Modulo by zero", expr),

            // Comparison (return 0 or 1)
            BinaryOperator.Less => left_i < right_i ? 1 : 0,
            BinaryOperator.LessEqual => left_i <= right_i ? 1 : 0,
            BinaryOperator.Greater => left_i > right_i ? 1 : 0,
            BinaryOperator.GreaterEqual => left_i >= right_i ? 1 : 0,
            BinaryOperator.Equal => left_i == right_i ? 1 : 0,
            BinaryOperator.NotEqual => left_i != right_i ? 1 : 0,

            // Bitwise
            BinaryOperator.BitwiseAnd => left_i & right_i,
            BinaryOperator.BitwiseOr => left_i | right_i,
            BinaryOperator.BitwiseXor => left_i ^ right_i,
            BinaryOperator.LeftShift => left_i << right_i,
            BinaryOperator.RightShift => left_i >> right_i,

            _ => throw new InterpreterException($"Unknown binary operator: {expr.Operator}", expr)
        };
    }

    private object EvaluateTernary(TernaryOp expr)
    {
        var condition = Evaluate(expr.Condition);
        return IsTrue(condition)
            ? Evaluate(expr.ThenBranch)
            : Evaluate(expr.ElseBranch);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Convert a value to int. In LPC, 0 and "" are false, everything else is true.
    /// </summary>
    private static int ToInt(object value, Expression context)
    {
        return value switch
        {
            int i => i,
            string s => throw new InterpreterException($"Cannot convert string to int", context),
            _ => throw new InterpreterException($"Unknown value type: {value.GetType().Name}", context)
        };
    }

    /// <summary>
    /// LPC truthiness: 0 is false, "" is false, everything else is true.
    /// </summary>
    private static bool IsTrue(object value)
    {
        return value switch
        {
            int i => i != 0,
            string s => s.Length > 0,
            _ => true
        };
    }

    /// <summary>
    /// Convert a value to string for concatenation.
    /// </summary>
    private static string ToString(object value)
    {
        return value switch
        {
            string s => s,
            int i => i.ToString(),
            _ => value.ToString() ?? ""
        };
    }

    #endregion
}

public class InterpreterException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public InterpreterException(string message, Expression expr)
        : base($"{message} at line {expr.Line}, column {expr.Column}")
    {
        Line = expr.Line;
        Column = expr.Column;
    }

    public InterpreterException(string message, Statement stmt)
        : base($"{message} at line {stmt.Line}, column {stmt.Column}")
    {
        Line = stmt.Line;
        Column = stmt.Column;
    }
}

/// <summary>
/// Thrown when a break statement is executed.
/// </summary>
public class BreakException : Exception { }

/// <summary>
/// Thrown when a continue statement is executed.
/// </summary>
public class ContinueException : Exception { }

/// <summary>
/// Thrown when a return statement is executed.
/// </summary>
public class ReturnException : Exception
{
    public object? Value { get; }

    public ReturnException(object? value)
    {
        Value = value;
    }
}
