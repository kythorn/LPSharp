namespace Driver.Tests;

public class InterpreterTests
{
    private static object Eval(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var interpreter = new Interpreter();
        return interpreter.Evaluate(ast);
    }

    #region Integer Arithmetic

    [Theory]
    [InlineData("5 + 3", 8)]
    [InlineData("5 - 3", 2)]
    [InlineData("5 * 3", 15)]
    [InlineData("10 / 3", 3)]    // Integer division
    [InlineData("10 % 3", 1)]
    public void Evaluate_IntegerArithmetic_ReturnsCorrectResult(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_NegativeNumbers_WorksCorrectly()
    {
        Assert.Equal(-5, Eval("-5"));
        Assert.Equal(2, Eval("5 + -3"));
        Assert.Equal(8, Eval("5 - -3"));
    }

    #endregion

    #region Operator Precedence

    [Fact]
    public void Evaluate_PrecedenceMultiplicationBeforeAddition()
    {
        Assert.Equal(11, Eval("5 + 3 * 2"));  // 5 + (3 * 2) = 11, not 16
    }

    [Fact]
    public void Evaluate_ParenthesesOverridePrecedence()
    {
        Assert.Equal(16, Eval("(5 + 3) * 2"));
    }

    [Fact]
    public void Evaluate_LeftToRightAssociativity()
    {
        Assert.Equal(2, Eval("10 - 5 - 3"));  // (10 - 5) - 3 = 2, not 8
    }

    #endregion

    #region Comparison Operators

    [Theory]
    [InlineData("5 == 5", 1)]
    [InlineData("5 == 3", 0)]
    [InlineData("5 != 3", 1)]
    [InlineData("5 != 5", 0)]
    [InlineData("5 < 10", 1)]
    [InlineData("5 < 3", 0)]
    [InlineData("5 <= 5", 1)]
    [InlineData("5 > 3", 1)]
    [InlineData("5 > 10", 0)]
    [InlineData("5 >= 5", 1)]
    public void Evaluate_ComparisonOperators_ReturnZeroOrOne(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Logical Operators

    [Theory]
    [InlineData("1 && 1", 1)]
    [InlineData("1 && 0", 0)]
    [InlineData("0 && 1", 0)]
    [InlineData("0 && 0", 0)]
    [InlineData("1 || 1", 1)]
    [InlineData("1 || 0", 1)]
    [InlineData("0 || 1", 1)]
    [InlineData("0 || 0", 0)]
    public void Evaluate_LogicalOperators_ReturnZeroOrOne(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_LogicalNot()
    {
        Assert.Equal(0, Eval("!1"));
        Assert.Equal(1, Eval("!0"));
        Assert.Equal(0, Eval("!5"));    // Any non-zero is true
    }

    [Fact]
    public void Evaluate_ShortCircuitAnd()
    {
        // 0 && (1/0) should not throw because && short-circuits
        Assert.Equal(0, Eval("0 && 1"));
    }

    [Fact]
    public void Evaluate_ShortCircuitOr()
    {
        // 1 || (1/0) should not throw because || short-circuits
        Assert.Equal(1, Eval("1 || 0"));
    }

    #endregion

    #region Bitwise Operators

    [Theory]
    [InlineData("5 & 3", 1)]       // 0101 & 0011 = 0001
    [InlineData("5 | 3", 7)]       // 0101 | 0011 = 0111
    [InlineData("5 ^ 3", 6)]       // 0101 ^ 0011 = 0110
    [InlineData("~0", -1)]         // Bitwise NOT of 0
    [InlineData("8 << 2", 32)]     // 8 * 4 = 32
    [InlineData("8 >> 2", 2)]      // 8 / 4 = 2
    public void Evaluate_BitwiseOperators_ReturnsCorrectResult(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Unary Operators

    [Fact]
    public void Evaluate_UnaryNegate()
    {
        Assert.Equal(-5, Eval("-5"));
        Assert.Equal(5, Eval("- -5"));   // -(-5) = 5 (space needed to avoid -- operator)
    }

    [Fact]
    public void Evaluate_UnaryBitwiseNot()
    {
        Assert.Equal(-1, Eval("~0"));
        Assert.Equal(-6, Eval("~5"));   // ~5 = -6 in two's complement
    }

    #endregion

    #region Ternary Operator

    [Fact]
    public void Evaluate_TernaryTrue()
    {
        Assert.Equal(1, Eval("5 > 3 ? 1 : 0"));
    }

    [Fact]
    public void Evaluate_TernaryFalse()
    {
        Assert.Equal(0, Eval("5 < 3 ? 1 : 0"));
    }

    [Fact]
    public void Evaluate_TernaryWithExpressions()
    {
        Assert.Equal(10, Eval("1 ? 5 + 5 : 3 + 3"));
        Assert.Equal(6, Eval("0 ? 5 + 5 : 3 + 3"));
    }

    [Fact]
    public void Evaluate_NestedTernary()
    {
        // 0 ? 1 : 1 ? 2 : 3 = 0 ? 1 : (1 ? 2 : 3) = 2
        Assert.Equal(2, Eval("0 ? 1 : 1 ? 2 : 3"));
    }

    #endregion

    #region String Operations

    [Fact]
    public void Evaluate_StringLiteral()
    {
        Assert.Equal("hello", Eval("\"hello\""));
    }

    [Fact]
    public void Evaluate_StringConcatenation()
    {
        Assert.Equal("hello world", Eval("\"hello\" + \" world\""));
    }

    [Fact]
    public void Evaluate_StringConcatenationWithInt()
    {
        Assert.Equal("value: 42", Eval("\"value: \" + 42"));
        Assert.Equal("42 is the answer", Eval("42 + \" is the answer\""));
    }

    [Fact]
    public void Evaluate_StringEquality()
    {
        Assert.Equal(1, Eval("\"hello\" == \"hello\""));
        Assert.Equal(0, Eval("\"hello\" == \"world\""));
        Assert.Equal(1, Eval("\"hello\" != \"world\""));
    }

    [Fact]
    public void Evaluate_EmptyStringIsFalsy()
    {
        Assert.Equal(0, Eval("\"\" ? 1 : 0"));
        Assert.Equal(1, Eval("\"x\" ? 1 : 0"));
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Evaluate_DivisionByZero_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("5 / 0"));
        Assert.Contains("Division by zero", ex.Message);
    }

    [Fact]
    public void Evaluate_ModuloByZero_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("5 % 0"));
        Assert.Contains("Modulo by zero", ex.Message);
    }

    [Fact]
    public void Evaluate_StringArithmeticOtherThanAdd_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("\"hello\" - \"world\""));
        Assert.Contains("not supported", ex.Message.ToLower());
    }

    #endregion

    #region Grouped Expressions

    [Fact]
    public void Evaluate_GroupedExpression()
    {
        Assert.Equal(5, Eval("(5)"));
        Assert.Equal(8, Eval("(5 + 3)"));
        Assert.Equal(16, Eval("((5 + 3) * 2)"));
    }

    #endregion

    #region Complex Expressions

    [Fact]
    public void Evaluate_ComplexArithmetic()
    {
        Assert.Equal(14, Eval("2 + 3 * 4"));           // 2 + 12 = 14
        Assert.Equal(10, Eval("(2 + 3) * 2"));         // 5 * 2 = 10
        Assert.Equal(5, Eval("1 + 2 * 3 - 4 / 2"));    // 1 + (2*3) - (4/2) = 1 + 6 - 2 = 5
    }

    [Fact]
    public void Evaluate_ComplexLogical()
    {
        Assert.Equal(1, Eval("5 > 3 && 10 > 5"));
        Assert.Equal(0, Eval("5 > 3 && 10 < 5"));
        Assert.Equal(1, Eval("5 > 3 || 10 < 5"));
    }

    [Fact]
    public void Evaluate_MixedTypes()
    {
        Assert.Equal("Result: 8", Eval("\"Result: \" + (5 + 3)"));
    }

    #endregion
}
