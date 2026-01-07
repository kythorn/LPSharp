namespace Driver.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_SingleNumber_ReturnsNumberToken()
    {
        var lexer = new Lexer("42");
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("42", tokens[0].Lexeme);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Fact]
    public void Tokenize_SimpleAddition_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("5 + 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("5", tokens[0].Lexeme);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal("3", tokens[2].Lexeme);
        Assert.Equal(TokenType.Eof, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_MultipleAdditions_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("1 + 2 + 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(6, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal(TokenType.Plus, tokens[3].Type);
        Assert.Equal(TokenType.Number, tokens[4].Type);
        Assert.Equal(TokenType.Eof, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_NoWhitespace_StillWorks()
    {
        var lexer = new Lexer("5+3");
        var tokens = lexer.Tokenize();

        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_TracksLineAndColumn()
    {
        var lexer = new Lexer("5 + 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column); // 5 at column 1
        Assert.Equal(3, tokens[1].Column); // + at column 3
        Assert.Equal(5, tokens[2].Column); // 3 at column 5
    }

    [Fact]
    public void Tokenize_EmptyInput_ReturnsOnlyEof()
    {
        var lexer = new Lexer("");
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsOnlyEof()
    {
        var lexer = new Lexer("   \t  \n  ");
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_MultilineInput_TracksLineNumbers()
    {
        var lexer = new Lexer("5\n+\n3");
        var tokens = lexer.Tokenize();

        Assert.Equal(4, tokens.Count);
        Assert.Equal(1, tokens[0].Line); // 5 on line 1
        Assert.Equal(2, tokens[1].Line); // + on line 2
        Assert.Equal(3, tokens[2].Line); // 3 on line 3
    }

    [Fact]
    public void Tokenize_MultiDigitNumbers_ParsedCorrectly()
    {
        var lexer = new Lexer("123 + 456789");
        var tokens = lexer.Tokenize();

        Assert.Equal("123", tokens[0].Lexeme);
        Assert.Equal("456789", tokens[2].Lexeme);
    }

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsException()
    {
        var lexer = new Lexer("5 @ 3");

        var ex = Assert.Throws<Exception>(() => lexer.Tokenize());
        Assert.Contains("@", ex.Message);
        Assert.Contains("line 1", ex.Message);
    }

    [Theory]
    [InlineData("+", TokenType.Plus)]
    [InlineData("-", TokenType.Minus)]
    [InlineData("*", TokenType.Star)]
    [InlineData("/", TokenType.Slash)]
    [InlineData("%", TokenType.Percent)]
    public void Tokenize_ArithmeticOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_MixedArithmetic_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("10 + 5 - 3 * 2 / 1 % 4");
        var tokens = lexer.Tokenize();

        Assert.Equal(12, tokens.Count); // 6 numbers + 5 operators + EOF
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal(TokenType.Minus, tokens[3].Type);
        Assert.Equal(TokenType.Number, tokens[4].Type);
        Assert.Equal(TokenType.Star, tokens[5].Type);
        Assert.Equal(TokenType.Number, tokens[6].Type);
        Assert.Equal(TokenType.Slash, tokens[7].Type);
        Assert.Equal(TokenType.Number, tokens[8].Type);
        Assert.Equal(TokenType.Percent, tokens[9].Type);
        Assert.Equal(TokenType.Number, tokens[10].Type);
        Assert.Equal(TokenType.Eof, tokens[11].Type);
    }
}
