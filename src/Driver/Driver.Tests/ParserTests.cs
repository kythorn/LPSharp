namespace Driver.Tests;

public class ParserTests
{
    private static Expression Parse(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        return parser.Parse();
    }

    #region Number Literals

    [Fact]
    public void Parse_SingleNumber_ReturnsNumberLiteral()
    {
        var expr = Parse("42");

        var num = Assert.IsType<NumberLiteral>(expr);
        Assert.Equal(42, num.Value);
    }

    [Fact]
    public void Parse_Zero_ReturnsNumberLiteral()
    {
        var expr = Parse("0");

        var num = Assert.IsType<NumberLiteral>(expr);
        Assert.Equal(0, num.Value);
    }

    #endregion

    #region String Literals

    [Fact]
    public void Parse_SimpleString_ReturnsStringLiteral()
    {
        var expr = Parse("\"hello\"");

        var str = Assert.IsType<StringLiteral>(expr);
        Assert.Equal("hello", str.Value);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsStringLiteral()
    {
        var expr = Parse("\"\"");

        var str = Assert.IsType<StringLiteral>(expr);
        Assert.Equal("", str.Value);
    }

    #endregion

    #region Arithmetic Operators

    [Fact]
    public void Parse_Addition_ReturnsBinaryOp()
    {
        var expr = Parse("5 + 3");

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(BinaryOperator.Add, bin.Operator);
        Assert.IsType<NumberLiteral>(bin.Left);
        Assert.IsType<NumberLiteral>(bin.Right);
    }

    [Theory]
    [InlineData("5 + 3", BinaryOperator.Add)]
    [InlineData("5 - 3", BinaryOperator.Subtract)]
    [InlineData("5 * 3", BinaryOperator.Multiply)]
    [InlineData("5 / 3", BinaryOperator.Divide)]
    [InlineData("5 % 3", BinaryOperator.Modulo)]
    public void Parse_ArithmeticOperators_ReturnsCorrectOperator(string source, BinaryOperator expected)
    {
        var expr = Parse(source);

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(expected, bin.Operator);
    }

    #endregion

    #region Operator Precedence

    [Fact]
    public void Parse_MultiplicationBeforeAddition_CorrectPrecedence()
    {
        // 5 + 3 * 2 should parse as 5 + (3 * 2)
        var expr = Parse("5 + 3 * 2");

        var add = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(BinaryOperator.Add, add.Operator);

        var left = Assert.IsType<NumberLiteral>(add.Left);
        Assert.Equal(5, left.Value);

        var mult = Assert.IsType<BinaryOp>(add.Right);
        Assert.Equal(BinaryOperator.Multiply, mult.Operator);
    }

    [Fact]
    public void Parse_ParenthesesOverridePrecedence()
    {
        // (5 + 3) * 2 should parse as (5 + 3) * 2
        var expr = Parse("(5 + 3) * 2");

        var mult = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(BinaryOperator.Multiply, mult.Operator);

        var grouped = Assert.IsType<GroupedExpression>(mult.Left);
        var add = Assert.IsType<BinaryOp>(grouped.Inner);
        Assert.Equal(BinaryOperator.Add, add.Operator);
    }

    [Fact]
    public void Parse_BitwiseAndBelowEquality_LpcPrecedence()
    {
        // In LPC: x & 1 == 0 parses as x & (1 == 0), NOT (x & 1) == 0
        // We'll test with literals: 5 & 1 == 1
        var expr = Parse("5 & 1 == 1");

        var bitAnd = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(BinaryOperator.BitwiseAnd, bitAnd.Operator);

        // Right side should be the equality check
        var eq = Assert.IsType<BinaryOp>(bitAnd.Right);
        Assert.Equal(BinaryOperator.Equal, eq.Operator);
    }

    [Fact]
    public void Parse_LogicalAndBeforeOr()
    {
        // a || b && c should parse as a || (b && c)
        var expr = Parse("1 || 2 && 3");

        var or = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(BinaryOperator.LogicalOr, or.Operator);

        var and = Assert.IsType<BinaryOp>(or.Right);
        Assert.Equal(BinaryOperator.LogicalAnd, and.Operator);
    }

    #endregion

    #region Comparison Operators

    [Theory]
    [InlineData("5 == 3", BinaryOperator.Equal)]
    [InlineData("5 != 3", BinaryOperator.NotEqual)]
    [InlineData("5 < 3", BinaryOperator.Less)]
    [InlineData("5 <= 3", BinaryOperator.LessEqual)]
    [InlineData("5 > 3", BinaryOperator.Greater)]
    [InlineData("5 >= 3", BinaryOperator.GreaterEqual)]
    public void Parse_ComparisonOperators_ReturnsCorrectOperator(string source, BinaryOperator expected)
    {
        var expr = Parse(source);

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(expected, bin.Operator);
    }

    #endregion

    #region Logical Operators

    [Theory]
    [InlineData("1 && 0", BinaryOperator.LogicalAnd)]
    [InlineData("1 || 0", BinaryOperator.LogicalOr)]
    public void Parse_LogicalOperators_ReturnsCorrectOperator(string source, BinaryOperator expected)
    {
        var expr = Parse(source);

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(expected, bin.Operator);
    }

    #endregion

    #region Bitwise Operators

    [Theory]
    [InlineData("5 & 3", BinaryOperator.BitwiseAnd)]
    [InlineData("5 | 3", BinaryOperator.BitwiseOr)]
    [InlineData("5 ^ 3", BinaryOperator.BitwiseXor)]
    [InlineData("5 << 2", BinaryOperator.LeftShift)]
    [InlineData("5 >> 2", BinaryOperator.RightShift)]
    public void Parse_BitwiseOperators_ReturnsCorrectOperator(string source, BinaryOperator expected)
    {
        var expr = Parse(source);

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(expected, bin.Operator);
    }

    #endregion

    #region Unary Operators

    [Fact]
    public void Parse_UnaryNegate_ReturnsUnaryOp()
    {
        var expr = Parse("-5");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.Negate, unary.Operator);
        Assert.True(unary.IsPrefix);
    }

    [Fact]
    public void Parse_UnaryLogicalNot_ReturnsUnaryOp()
    {
        var expr = Parse("!1");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.LogicalNot, unary.Operator);
    }

    [Fact]
    public void Parse_UnaryBitwiseNot_ReturnsUnaryOp()
    {
        var expr = Parse("~5");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.BitwiseNot, unary.Operator);
    }

    [Fact]
    public void Parse_ChainedUnary_RightToLeft()
    {
        // - -5 (with space) should parse as -(-5)
        // Note: --5 without space is the decrement operator
        var expr = Parse("- -5");

        var outer = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.Negate, outer.Operator);

        var inner = Assert.IsType<UnaryOp>(outer.Operand);
        Assert.Equal(UnaryOperator.Negate, inner.Operator);
    }

    #endregion

    #region Ternary Operator

    [Fact]
    public void Parse_Ternary_ReturnsTernaryOp()
    {
        var expr = Parse("1 ? 2 : 3");

        var ternary = Assert.IsType<TernaryOp>(expr);
        Assert.IsType<NumberLiteral>(ternary.Condition);
        Assert.IsType<NumberLiteral>(ternary.ThenBranch);
        Assert.IsType<NumberLiteral>(ternary.ElseBranch);
    }

    [Fact]
    public void Parse_NestedTernary_RightToLeft()
    {
        // a ? b : c ? d : e parses as a ? b : (c ? d : e)
        var expr = Parse("1 ? 2 : 3 ? 4 : 5");

        var outer = Assert.IsType<TernaryOp>(expr);
        var inner = Assert.IsType<TernaryOp>(outer.ElseBranch);
        Assert.IsType<NumberLiteral>(inner.Condition);
    }

    #endregion

    #region Grouped Expressions

    [Fact]
    public void Parse_GroupedExpression_ReturnsGroupedExpression()
    {
        var expr = Parse("(5)");

        var grouped = Assert.IsType<GroupedExpression>(expr);
        Assert.IsType<NumberLiteral>(grouped.Inner);
    }

    [Fact]
    public void Parse_NestedGroups_ParsesCorrectly()
    {
        var expr = Parse("((5 + 3))");

        var outer = Assert.IsType<GroupedExpression>(expr);
        var inner = Assert.IsType<GroupedExpression>(outer.Inner);
        Assert.IsType<BinaryOp>(inner.Inner);
    }

    #endregion

    #region Identifiers and Assignment

    [Fact]
    public void Parse_Identifier_ReturnsIdentifier()
    {
        var expr = Parse("foo");

        var id = Assert.IsType<Identifier>(expr);
        Assert.Equal("foo", id.Name);
    }

    [Fact]
    public void Parse_Assignment_ReturnsAssignment()
    {
        var expr = Parse("x = 5");

        var assign = Assert.IsType<Assignment>(expr);
        Assert.Equal("x", assign.Name);
        Assert.IsType<NumberLiteral>(assign.Value);
    }

    [Fact]
    public void Parse_ChainedAssignment_RightToLeft()
    {
        // x = y = 5 should parse as x = (y = 5)
        var expr = Parse("x = y = 5");

        var outer = Assert.IsType<Assignment>(expr);
        Assert.Equal("x", outer.Name);

        var inner = Assert.IsType<Assignment>(outer.Value);
        Assert.Equal("y", inner.Name);
        Assert.IsType<NumberLiteral>(inner.Value);
    }

    [Fact]
    public void Parse_IdentifierInExpression_Works()
    {
        var expr = Parse("x + y * 2");

        var add = Assert.IsType<BinaryOp>(expr);
        Assert.IsType<Identifier>(add.Left);

        var mult = Assert.IsType<BinaryOp>(add.Right);
        Assert.IsType<Identifier>(mult.Left);
    }

    [Fact]
    public void Parse_AssignmentWithExpression_Works()
    {
        var expr = Parse("x = 5 + 3");

        var assign = Assert.IsType<Assignment>(expr);
        Assert.Equal("x", assign.Name);
        Assert.IsType<BinaryOp>(assign.Value);
    }

    [Fact]
    public void Parse_AssignmentPrecedenceBelowTernary()
    {
        // x = a ? b : c should parse as x = (a ? b : c)
        var expr = Parse("x = 1 ? 2 : 3");

        var assign = Assert.IsType<Assignment>(expr);
        Assert.Equal("x", assign.Name);
        Assert.IsType<TernaryOp>(assign.Value);
    }

    [Fact]
    public void Parse_InvalidAssignmentTarget_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("5 = 3"));
        Assert.Contains("Invalid assignment target", ex.Message);
    }

    #endregion

    #region Compound Assignment

    [Theory]
    [InlineData("x += 5", BinaryOperator.Add)]
    [InlineData("x -= 5", BinaryOperator.Subtract)]
    [InlineData("x *= 5", BinaryOperator.Multiply)]
    [InlineData("x /= 5", BinaryOperator.Divide)]
    [InlineData("x %= 5", BinaryOperator.Modulo)]
    [InlineData("x &= 5", BinaryOperator.BitwiseAnd)]
    [InlineData("x |= 5", BinaryOperator.BitwiseOr)]
    [InlineData("x ^= 5", BinaryOperator.BitwiseXor)]
    [InlineData("x <<= 2", BinaryOperator.LeftShift)]
    [InlineData("x >>= 2", BinaryOperator.RightShift)]
    public void Parse_CompoundAssignment_ReturnsCorrectOperator(string source, BinaryOperator expected)
    {
        var expr = Parse(source);

        var assign = Assert.IsType<CompoundAssignment>(expr);
        Assert.Equal("x", assign.Name);
        Assert.Equal(expected, assign.Operator);
    }

    [Fact]
    public void Parse_CompoundAssignmentWithExpression_Works()
    {
        var expr = Parse("x += 2 * 3");

        var assign = Assert.IsType<CompoundAssignment>(expr);
        Assert.Equal("x", assign.Name);
        Assert.Equal(BinaryOperator.Add, assign.Operator);
        Assert.IsType<BinaryOp>(assign.Value);
    }

    [Fact]
    public void Parse_InvalidCompoundAssignmentTarget_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("5 += 3"));
        Assert.Contains("Invalid assignment target", ex.Message);
    }

    #endregion

    #region Increment/Decrement

    [Fact]
    public void Parse_PrefixIncrement_ReturnsUnaryOp()
    {
        var expr = Parse("++x");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.PreIncrement, unary.Operator);
        Assert.True(unary.IsPrefix);
        var id = Assert.IsType<Identifier>(unary.Operand);
        Assert.Equal("x", id.Name);
    }

    [Fact]
    public void Parse_PrefixDecrement_ReturnsUnaryOp()
    {
        var expr = Parse("--x");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.PreDecrement, unary.Operator);
        Assert.True(unary.IsPrefix);
    }

    [Fact]
    public void Parse_PostfixIncrement_ReturnsUnaryOp()
    {
        var expr = Parse("x++");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.PostIncrement, unary.Operator);
        Assert.False(unary.IsPrefix);
        var id = Assert.IsType<Identifier>(unary.Operand);
        Assert.Equal("x", id.Name);
    }

    [Fact]
    public void Parse_PostfixDecrement_ReturnsUnaryOp()
    {
        var expr = Parse("x--");

        var unary = Assert.IsType<UnaryOp>(expr);
        Assert.Equal(UnaryOperator.PostDecrement, unary.Operator);
        Assert.False(unary.IsPrefix);
    }

    [Fact]
    public void Parse_IncrementInExpression_Works()
    {
        // x++ + 1 should parse as (x++) + 1
        var expr = Parse("x++ + 1");

        var add = Assert.IsType<BinaryOp>(expr);
        Assert.IsType<UnaryOp>(add.Left);
        Assert.IsType<NumberLiteral>(add.Right);
    }

    [Fact]
    public void Parse_PrefixIncrementOnLiteral_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("++5"));
        Assert.Contains("requires a variable", ex.Message);
    }

    [Fact]
    public void Parse_PostfixIncrementOnLiteral_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("5++"));
        Assert.Contains("requires a variable", ex.Message);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Parse_UnexpectedToken_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("5 +"));
        Assert.Contains("Expected expression", ex.Message);
    }

    [Fact]
    public void Parse_UnclosedParen_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("(5 + 3"));
        Assert.Contains("')'", ex.Message);
    }

    [Fact]
    public void Parse_MissingTernaryColon_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("1 ? 2"));
        Assert.Contains("':'", ex.Message);
    }

    [Fact]
    public void Parse_ExtraTokens_ThrowsParserException()
    {
        var ex = Assert.Throws<ParserException>(() => Parse("5 3"));
        Assert.Contains("Unexpected token", ex.Message);
    }

    #endregion

    #region Line/Column Tracking

    [Fact]
    public void Parse_TracksPosition()
    {
        var expr = Parse("5 + 3");

        var bin = Assert.IsType<BinaryOp>(expr);
        Assert.Equal(1, bin.Line);
        Assert.Equal(3, bin.Column); // Position of the + operator
    }

    #endregion
}
