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
/// LPC integers are 64-bit to support large values (XP, gold, etc.)
/// </summary>
public record NumberLiteral(long Value) : Expression;

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
/// Function call: name(arg1, arg2, ...) or ::name(arg1, arg2, ...)
/// IsParentCall=true indicates :: prefix (call parent/inherited version)
/// </summary>
public record FunctionCall(string Name, List<Expression> Arguments, bool IsParentCall = false) : Expression;

/// <summary>
/// Arrow call: obj->func(arg1, arg2, ...)
/// Syntactic sugar for call_other(obj, "func", args...)
/// </summary>
public record ArrowCall(Expression Target, string FunctionName, List<Expression> Arguments) : Expression;

/// <summary>
/// Array literal: ({ expr1, expr2, ... })
/// </summary>
public record ArrayLiteral(List<Expression> Elements) : Expression;

/// <summary>
/// Mapping literal: ([ key1: val1, key2: val2, ... ])
/// </summary>
public record MappingLiteral(List<(Expression Key, Expression Value)> Entries) : Expression;

/// <summary>
/// Array/string indexing: expr[index]
/// </summary>
public record IndexExpression(Expression Target, Expression Index) : Expression;

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

// ============================================================================
// STATEMENTS
// ============================================================================

/// <summary>
/// Base class for all AST statement nodes.
/// </summary>
public abstract record Statement
{
    public int Line { get; init; }
    public int Column { get; init; }
}

/// <summary>
/// Block statement: { stmt1; stmt2; ... }
/// </summary>
public record BlockStatement(List<Statement> Statements) : Statement;

/// <summary>
/// Expression statement: expr;
/// Wraps an expression to be executed as a statement.
/// </summary>
public record ExpressionStatement(Expression Expression) : Statement;

/// <summary>
/// If statement: if (condition) thenBranch else elseBranch
/// </summary>
public record IfStatement(Expression Condition, Statement ThenBranch, Statement? ElseBranch) : Statement;

/// <summary>
/// While statement: while (condition) body
/// </summary>
public record WhileStatement(Expression Condition, Statement Body) : Statement;

/// <summary>
/// For statement: for (init; condition; increment) body
/// </summary>
public record ForStatement(Expression? Init, Expression? Condition, Expression? Increment, Statement Body) : Statement;

/// <summary>
/// Switch statement: switch (expr) { case val: ... default: ... }
/// </summary>
public record SwitchStatement(Expression Value, List<SwitchCase> Cases) : Statement;

/// <summary>
/// A case in a switch statement. Value is null for 'default:'.
/// </summary>
public record SwitchCase(Expression? Value, List<Statement> Statements);

/// <summary>
/// Foreach statement: foreach (variable in collection) body
/// </summary>
public record ForEachStatement(string Variable, Expression Collection, Statement Body) : Statement;

/// <summary>
/// Break statement: break;
/// </summary>
public record BreakStatement : Statement;

/// <summary>
/// Continue statement: continue;
/// </summary>
public record ContinueStatement : Statement;

/// <summary>
/// Return statement: return expr; or return;
/// </summary>
public record ReturnStatement(Expression? Value) : Statement;

/// <summary>
/// Inherit statement: inherit "/path/to/file";
/// Loads and inherits from the specified object.
/// </summary>
public record InheritStatement(string Path) : Statement;

/// <summary>
/// Variable declaration: type name;  or  type name = value;
/// Examples: int damage;  string name = "sword";
/// </summary>
public record VariableDeclaration(string Type, string Name, Expression? Initializer) : Statement;

/// <summary>
/// Function definition: type name(params) { body }
/// Type is stored as string for now (int, string, void, object, etc.)
/// Parameters are stored as strings for now (will add types later).
/// </summary>
public record FunctionDefinition(string ReturnType, string Name, List<string> Parameters, Statement Body) : Statement;
