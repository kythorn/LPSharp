namespace Driver;

/// <summary>
/// Base class for all AST expression nodes.
/// </summary>
public abstract record Expression
{
    public int Line { get; init; }
    public int Column { get; init; }
}

/// <summary>
/// Integer literal: 42
/// </summary>
public record NumberLiteral(int Value) : Expression;

/// <summary>
/// String literal: "hello"
/// </summary>
public record StringLiteral(string Value) : Expression;

/// <summary>
/// Binary operation: left op right
/// </summary>
public record BinaryOp(Expression Left, BinaryOperator Operator, Expression Right) : Expression;

/// <summary>
/// Unary operation: op operand (prefix) or operand op (postfix)
/// </summary>
public record UnaryOp(UnaryOperator Operator, Expression Operand, bool IsPrefix = true) : Expression;

/// <summary>
/// Grouped expression: (expr)
/// </summary>
public record GroupedExpression(Expression Inner) : Expression;

/// <summary>
/// Ternary conditional: condition ? thenExpr : elseExpr
/// </summary>
public record TernaryOp(Expression Condition, Expression ThenBranch, Expression ElseBranch) : Expression;

/// <summary>
/// Variable reference: x
/// </summary>
public record Identifier(string Name) : Expression;

/// <summary>
/// Assignment: name = value
/// </summary>
public record Assignment(string Name, Expression Value) : Expression;

/// <summary>
/// Compound assignment: name op= value (e.g., x += 5)
/// </summary>
public record CompoundAssignment(string Name, BinaryOperator Operator, Expression Value) : Expression;

/// <summary>
/// Binary operators with their precedence levels (higher = binds tighter).
/// Follows authentic LDMud precedence where bitwise ops are below comparison.
/// </summary>
public enum BinaryOperator
{
    // Precedence 4: Multiplicative
    Multiply,
    Divide,
    Modulo,

    // Precedence 5: Additive
    Add,
    Subtract,

    // Precedence 6: Shift
    LeftShift,
    RightShift,

    // Precedence 7: Relational
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Precedence 8: Equality
    Equal,
    NotEqual,

    // Precedence 9: Bitwise AND
    BitwiseAnd,

    // Precedence 10: Bitwise XOR
    BitwiseXor,

    // Precedence 11: Bitwise OR
    BitwiseOr,

    // Precedence 12: Logical AND
    LogicalAnd,

    // Precedence 13: Logical OR
    LogicalOr,
}

/// <summary>
/// Unary operators.
/// </summary>
public enum UnaryOperator
{
    // Prefix operators
    Negate,         // -x
    LogicalNot,     // !x
    BitwiseNot,     // ~x
    PreIncrement,   // ++x
    PreDecrement,   // --x
    PostIncrement,  // x++
    PostDecrement,  // x--
}
