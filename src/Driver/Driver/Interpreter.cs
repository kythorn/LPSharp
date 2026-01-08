namespace Driver;

/// <summary>
/// Tree-walking interpreter for LPC expressions.
/// Evaluates AST nodes and returns results.
/// </summary>
public class Interpreter
{
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
            _ => throw new InterpreterException($"Unknown expression type: {expr.GetType().Name}", expr)
        };
    }

    private object EvaluateUnary(UnaryOp expr)
    {
        var operand = Evaluate(expr.Operand);

        return expr.Operator switch
        {
            UnaryOperator.Negate => -(ToInt(operand, expr)),
            UnaryOperator.LogicalNot => IsTrue(operand) ? 0 : 1,
            UnaryOperator.BitwiseNot => ~ToInt(operand, expr),
            _ => throw new InterpreterException($"Unknown unary operator: {expr.Operator}", expr)
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
}
