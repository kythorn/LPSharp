namespace Driver.Tests;

public class LexerTests
{
    #region Numbers

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
    public void Tokenize_MultiDigitNumbers_ParsedCorrectly()
    {
        var lexer = new Lexer("123 456789");
        var tokens = lexer.Tokenize();

        Assert.Equal("123", tokens[0].Lexeme);
        Assert.Equal("456789", tokens[1].Lexeme);
    }

    #endregion

    #region Arithmetic Operators

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

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_MixedArithmetic_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("10 + 5 - 3 * 2 / 1 % 4");
        var tokens = lexer.Tokenize();

        Assert.Equal(12, tokens.Count); // 6 numbers + 5 operators + EOF
    }

    #endregion

    #region Comparison Operators

    [Theory]
    [InlineData("==", TokenType.EqualEqual)]
    [InlineData("!=", TokenType.BangEqual)]
    [InlineData("<", TokenType.Less)]
    [InlineData(">", TokenType.Greater)]
    [InlineData("<=", TokenType.LessEqual)]
    [InlineData(">=", TokenType.GreaterEqual)]
    public void Tokenize_ComparisonOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    #endregion

    #region Logical Operators

    [Theory]
    [InlineData("&&", TokenType.AmpAmp)]
    [InlineData("||", TokenType.PipePipe)]
    [InlineData("!", TokenType.Bang)]
    public void Tokenize_LogicalOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    #endregion

    #region Bitwise Operators

    [Theory]
    [InlineData("&", TokenType.Amp)]
    [InlineData("|", TokenType.Pipe)]
    [InlineData("^", TokenType.Caret)]
    [InlineData("~", TokenType.Tilde)]
    [InlineData("<<", TokenType.LessLess)]
    [InlineData(">>", TokenType.GreaterGreater)]
    public void Tokenize_BitwiseOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    #endregion

    #region Assignment Operators

    [Theory]
    [InlineData("=", TokenType.Equal)]
    [InlineData("+=", TokenType.PlusEqual)]
    [InlineData("-=", TokenType.MinusEqual)]
    [InlineData("*=", TokenType.StarEqual)]
    [InlineData("/=", TokenType.SlashEqual)]
    [InlineData("%=", TokenType.PercentEqual)]
    [InlineData("&=", TokenType.AmpEqual)]
    [InlineData("|=", TokenType.PipeEqual)]
    [InlineData("^=", TokenType.CaretEqual)]
    [InlineData("<<=", TokenType.LessLessEqual)]
    [InlineData(">>=", TokenType.GreaterGreaterEqual)]
    public void Tokenize_AssignmentOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    #endregion

    #region Increment/Decrement

    [Theory]
    [InlineData("++", TokenType.PlusPlus)]
    [InlineData("--", TokenType.MinusMinus)]
    public void Tokenize_IncrementDecrement_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_IncrementInExpression_ParsesCorrectly()
    {
        var lexer = new Lexer("x++");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.PlusPlus, tokens[1].Type);
    }

    #endregion

    #region Other Operators

    [Theory]
    [InlineData("?", TokenType.Question)]
    [InlineData(":", TokenType.Colon)]
    [InlineData("::", TokenType.ColonColon)]
    [InlineData("->", TokenType.Arrow)]
    public void Tokenize_OtherOperators_ReturnsCorrectType(string op, TokenType expected)
    {
        var lexer = new Lexer(op);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(op, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_TernaryExpression_ParsesCorrectly()
    {
        var lexer = new Lexer("a ? b : c");
        var tokens = lexer.Tokenize();

        Assert.Equal(6, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.Question, tokens[1].Type);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
        Assert.Equal(TokenType.Colon, tokens[3].Type);
        Assert.Equal(TokenType.Identifier, tokens[4].Type);
    }

    [Fact]
    public void Tokenize_ParentCall_ParsesColonColon()
    {
        var lexer = new Lexer("::create()");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.ColonColon, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("create", tokens[1].Lexeme);
    }

    [Fact]
    public void Tokenize_ArrowCall_ParsesCorrectly()
    {
        var lexer = new Lexer("player->tell(msg)");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.Arrow, tokens[1].Type);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
    }

    #endregion

    #region Delimiters

    [Theory]
    [InlineData("(", TokenType.LeftParen)]
    [InlineData(")", TokenType.RightParen)]
    [InlineData("{", TokenType.LeftBrace)]
    [InlineData("}", TokenType.RightBrace)]
    [InlineData("[", TokenType.LeftBracket)]
    [InlineData("]", TokenType.RightBracket)]
    [InlineData(";", TokenType.Semicolon)]
    [InlineData(",", TokenType.Comma)]
    public void Tokenize_Delimiters_ReturnsCorrectType(string delim, TokenType expected)
    {
        var lexer = new Lexer(delim);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_FunctionCall_ParsesDelimiters()
    {
        var lexer = new Lexer("func(a, b)");
        var tokens = lexer.Tokenize();

        Assert.Equal(7, tokens.Count); // func ( a , b ) EOF
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.LeftParen, tokens[1].Type);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
        Assert.Equal(TokenType.Comma, tokens[3].Type);
        Assert.Equal(TokenType.Identifier, tokens[4].Type);
        Assert.Equal(TokenType.RightParen, tokens[5].Type);
        Assert.Equal(TokenType.Eof, tokens[6].Type);
    }

    #endregion

    #region Keywords

    [Theory]
    [InlineData("if", TokenType.If)]
    [InlineData("else", TokenType.Else)]
    [InlineData("while", TokenType.While)]
    [InlineData("for", TokenType.For)]
    [InlineData("return", TokenType.Return)]
    [InlineData("inherit", TokenType.Inherit)]
    [InlineData("int", TokenType.Int)]
    [InlineData("string", TokenType.StringType)]
    [InlineData("object", TokenType.Object)]
    [InlineData("mapping", TokenType.Mapping)]
    [InlineData("mixed", TokenType.Mixed)]
    [InlineData("void", TokenType.Void)]
    public void Tokenize_Keywords_ReturnsCorrectType(string keyword, TokenType expected)
    {
        var lexer = new Lexer(keyword);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(keyword, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_KeywordAsPartOfIdentifier_ReturnsIdentifier()
    {
        var lexer = new Lexer("ifStatement");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("ifStatement", tokens[0].Lexeme);
    }

    #endregion

    #region Identifiers

    [Theory]
    [InlineData("x")]
    [InlineData("foo")]
    [InlineData("myVariable")]
    [InlineData("_private")]
    [InlineData("__init__")]
    [InlineData("var123")]
    [InlineData("CamelCase")]
    public void Tokenize_Identifiers_ReturnsIdentifierToken(string id)
    {
        var lexer = new Lexer(id);
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(id, tokens[0].Lexeme);
    }

    #endregion

    #region String Literals

    [Fact]
    public void Tokenize_SimpleString_ReturnsStringToken()
    {
        var lexer = new Lexer("\"hello\"");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsEmptyStringToken()
    {
        var lexer = new Lexer("\"\"");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_StringWithSpaces_PreservesSpaces()
    {
        var lexer = new Lexer("\"hello world\"");
        var tokens = lexer.Tokenize();

        Assert.Equal("hello world", tokens[0].Lexeme);
    }

    [Theory]
    [InlineData("\"hello\\nworld\"", "hello\nworld")]
    [InlineData("\"tab\\there\"", "tab\there")]
    [InlineData("\"return\\rhere\"", "return\rhere")]
    [InlineData("\"quote\\\"here\"", "quote\"here")]
    [InlineData("\"backslash\\\\here\"", "backslash\\here")]
    [InlineData("\"null\\0here\"", "null\0here")]
    public void Tokenize_StringEscapeSequences_ParsesCorrectly(string input, string expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Equal(expected, tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ThrowsException()
    {
        var lexer = new Lexer("\"unterminated");

        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("Unterminated string", ex.Message);
    }

    [Fact]
    public void Tokenize_StringWithNewline_ThrowsException()
    {
        var lexer = new Lexer("\"hello\nworld\"");

        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("newline", ex.Message.ToLower());
    }

    [Fact]
    public void Tokenize_InvalidEscapeSequence_ThrowsException()
    {
        var lexer = new Lexer("\"invalid\\x\"");

        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("escape", ex.Message.ToLower());
    }

    #endregion

    #region Comments

    [Fact]
    public void Tokenize_SingleLineComment_SkipsComment()
    {
        var lexer = new Lexer("5 // this is a comment\n+ 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_SingleLineCommentAtEnd_SkipsComment()
    {
        var lexer = new Lexer("42 // comment at end");
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("42", tokens[0].Lexeme);
    }

    [Fact]
    public void Tokenize_MultiLineComment_SkipsComment()
    {
        var lexer = new Lexer("5 /* this is\na multi-line\ncomment */ + 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(4, tokens.Count);
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(TokenType.Plus, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_UnterminatedBlockComment_ThrowsException()
    {
        var lexer = new Lexer("5 /* unterminated");

        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("block comment", ex.Message.ToLower());
    }

    [Fact]
    public void Tokenize_NestedStyleBlockComment_NotSupported()
    {
        // LPC doesn't support nested comments - the first */ ends the comment
        var lexer = new Lexer("/* outer /* inner */ still comment? */ 5");
        var tokens = lexer.Tokenize();

        // After "/* outer /* inner */" the comment ends, then "still comment? */" is tokens
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("still", tokens[0].Lexeme);
    }

    #endregion

    #region Whitespace and Line Tracking

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
    public void Tokenize_TracksLineAndColumn()
    {
        var lexer = new Lexer("5 + 3");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(3, tokens[1].Column);
        Assert.Equal(5, tokens[2].Column);
    }

    [Fact]
    public void Tokenize_MultilineInput_TracksLineNumbers()
    {
        var lexer = new Lexer("5\n+\n3");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal(3, tokens[2].Line);
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

    #endregion

    #region Error Handling

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsException()
    {
        var lexer = new Lexer("5 @ 3");

        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("@", ex.Message);
        Assert.Equal(1, ex.Line);
        Assert.Equal(3, ex.Column);
    }

    #endregion

    #region Complex LPC Code

    [Fact]
    public void Tokenize_FunctionDefinition_ParsesCorrectly()
    {
        var lexer = new Lexer("int add(int a, int b) { return a + b; }");
        var tokens = lexer.Tokenize();

        var expectedTypes = new[]
        {
            TokenType.Int, TokenType.Identifier, TokenType.LeftParen,
            TokenType.Int, TokenType.Identifier, TokenType.Comma,
            TokenType.Int, TokenType.Identifier, TokenType.RightParen,
            TokenType.LeftBrace, TokenType.Return, TokenType.Identifier,
            TokenType.Plus, TokenType.Identifier, TokenType.Semicolon,
            TokenType.RightBrace, TokenType.Eof
        };

        Assert.Equal(expectedTypes.Length, tokens.Count);
        for (int i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], tokens[i].Type);
        }
    }

    [Fact]
    public void Tokenize_InheritStatement_ParsesCorrectly()
    {
        var lexer = new Lexer("inherit \"/std/object\";");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.Inherit, tokens[0].Type);
        Assert.Equal(TokenType.String, tokens[1].Type);
        Assert.Equal("/std/object", tokens[1].Lexeme);
        Assert.Equal(TokenType.Semicolon, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_IfStatement_ParsesCorrectly()
    {
        var lexer = new Lexer("if (x > 0) { return 1; } else { return 0; }");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.If, tokens[0].Type);
        Assert.Equal(TokenType.LeftParen, tokens[1].Type);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
        Assert.Equal(TokenType.Greater, tokens[3].Type);
    }

    [Fact]
    public void Tokenize_WhileLoop_ParsesCorrectly()
    {
        var lexer = new Lexer("while (i < 10) { i++; }");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.While, tokens[0].Type);
        Assert.Contains(tokens, t => t.Type == TokenType.PlusPlus);
    }

    [Fact]
    public void Tokenize_ForLoop_ParsesCorrectly()
    {
        var lexer = new Lexer("for (int i = 0; i < 10; i++) { }");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.For, tokens[0].Type);
        Assert.Contains(tokens, t => t.Type == TokenType.LessEqual || t.Type == TokenType.Less);
    }

    #endregion
}
