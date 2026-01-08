namespace Driver;

/// <summary>
/// Recursive descent parser for LPC expressions.
/// Implements authentic LDMud operator precedence.
/// </summary>
public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Expression Parse()
    {
        var expr = ParseExpression();

        if (!IsAtEnd() && Current().Type != TokenType.Eof)
        {
            throw new ParserException($"Unexpected token '{Current().Lexeme}'", Current());
        }

        return expr;
    }

    private Expression ParseExpression()
    {
        return ParseTernary();
    }

    // Precedence 14: Ternary conditional (right-to-left)
    private Expression ParseTernary()
    {
        var condition = ParseLogicalOr();

        if (Match(TokenType.Question))
        {
            var questionToken = Previous();
            var thenBranch = ParseTernary(); // Right-to-left associativity

            if (!Match(TokenType.Colon))
            {
                throw new ParserException("Expected ':' in ternary expression", Current());
            }

            var elseBranch = ParseTernary(); // Right-to-left associativity

            return new TernaryOp(condition, thenBranch, elseBranch)
            {
                Line = questionToken.Line,
                Column = questionToken.Column
            };
        }

        return condition;
    }

    // Precedence 13: || (lowest binary precedence we handle)
    private Expression ParseLogicalOr()
    {
        var left = ParseLogicalAnd();

        while (Match(TokenType.PipePipe))
        {
            var op = Previous();
            var right = ParseLogicalAnd();
            left = new BinaryOp(left, BinaryOperator.LogicalOr, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 12: &&
    private Expression ParseLogicalAnd()
    {
        var left = ParseBitwiseOr();

        while (Match(TokenType.AmpAmp))
        {
            var op = Previous();
            var right = ParseBitwiseOr();
            left = new BinaryOp(left, BinaryOperator.LogicalAnd, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 11: |
    private Expression ParseBitwiseOr()
    {
        var left = ParseBitwiseXor();

        while (Match(TokenType.Pipe))
        {
            var op = Previous();
            var right = ParseBitwiseXor();
            left = new BinaryOp(left, BinaryOperator.BitwiseOr, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 10: ^
    private Expression ParseBitwiseXor()
    {
        var left = ParseBitwiseAnd();

        while (Match(TokenType.Caret))
        {
            var op = Previous();
            var right = ParseBitwiseAnd();
            left = new BinaryOp(left, BinaryOperator.BitwiseXor, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 9: &
    private Expression ParseBitwiseAnd()
    {
        var left = ParseEquality();

        while (Match(TokenType.Amp))
        {
            var op = Previous();
            var right = ParseEquality();
            left = new BinaryOp(left, BinaryOperator.BitwiseAnd, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 8: == !=
    private Expression ParseEquality()
    {
        var left = ParseRelational();

        while (Match(TokenType.EqualEqual, TokenType.BangEqual))
        {
            var op = Previous();
            var binOp = op.Type == TokenType.EqualEqual ? BinaryOperator.Equal : BinaryOperator.NotEqual;
            var right = ParseRelational();
            left = new BinaryOp(left, binOp, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 7: < <= > >=
    private Expression ParseRelational()
    {
        var left = ParseShift();

        while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
        {
            var op = Previous();
            var binOp = op.Type switch
            {
                TokenType.Less => BinaryOperator.Less,
                TokenType.LessEqual => BinaryOperator.LessEqual,
                TokenType.Greater => BinaryOperator.Greater,
                TokenType.GreaterEqual => BinaryOperator.GreaterEqual,
                _ => throw new ParserException($"Unexpected relational operator", op)
            };
            var right = ParseShift();
            left = new BinaryOp(left, binOp, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 6: << >>
    private Expression ParseShift()
    {
        var left = ParseAdditive();

        while (Match(TokenType.LessLess, TokenType.GreaterGreater))
        {
            var op = Previous();
            var binOp = op.Type == TokenType.LessLess ? BinaryOperator.LeftShift : BinaryOperator.RightShift;
            var right = ParseAdditive();
            left = new BinaryOp(left, binOp, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 5: + -
    private Expression ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous();
            var binOp = op.Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            var right = ParseMultiplicative();
            left = new BinaryOp(left, binOp, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 4: * / %
    private Expression ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Match(TokenType.Star, TokenType.Slash, TokenType.Percent))
        {
            var op = Previous();
            var binOp = op.Type switch
            {
                TokenType.Star => BinaryOperator.Multiply,
                TokenType.Slash => BinaryOperator.Divide,
                TokenType.Percent => BinaryOperator.Modulo,
                _ => throw new ParserException($"Unexpected multiplicative operator", op)
            };
            var right = ParseUnary();
            left = new BinaryOp(left, binOp, right)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    // Precedence 3: Unary prefix operators: - ! ~
    private Expression ParseUnary()
    {
        if (Match(TokenType.Minus, TokenType.Bang, TokenType.Tilde))
        {
            var op = Previous();
            var unaryOp = op.Type switch
            {
                TokenType.Minus => UnaryOperator.Negate,
                TokenType.Bang => UnaryOperator.LogicalNot,
                TokenType.Tilde => UnaryOperator.BitwiseNot,
                _ => throw new ParserException($"Unexpected unary operator", op)
            };
            var operand = ParseUnary(); // Right-to-left associativity
            return new UnaryOp(unaryOp, operand)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return ParsePrimary();
    }

    // Precedence 1: Primary expressions (literals, grouping)
    private Expression ParsePrimary()
    {
        // Number literal
        if (Match(TokenType.Number))
        {
            var token = Previous();
            if (!int.TryParse(token.Lexeme, out int value))
            {
                throw new ParserException($"Invalid number '{token.Lexeme}'", token);
            }
            return new NumberLiteral(value)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        // String literal
        if (Match(TokenType.String))
        {
            var token = Previous();
            return new StringLiteral(token.Lexeme)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        // Grouped expression: (expr)
        if (Match(TokenType.LeftParen))
        {
            var openParen = Previous();
            var expr = ParseExpression();

            if (!Match(TokenType.RightParen))
            {
                throw new ParserException("Expected ')' after expression", Current());
            }

            return new GroupedExpression(expr)
            {
                Line = openParen.Line,
                Column = openParen.Column
            };
        }

        // Ternary: We handle it here for now since it's the lowest binary precedence
        // Actually, ternary should be handled at a higher level... let's add it
        throw new ParserException($"Expected expression, got '{Current().Lexeme}'", Current());
    }

    #region Helper Methods

    private Token Current()
    {
        if (_position >= _tokens.Count)
        {
            return _tokens[^1]; // Return EOF token
        }
        return _tokens[_position];
    }

    private Token Previous()
    {
        return _tokens[_position - 1];
    }

    private bool IsAtEnd()
    {
        return _position >= _tokens.Count || Current().Type == TokenType.Eof;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Current().Type == type;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _position++;
        }
        return Previous();
    }

    #endregion
}

public class ParserException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParserException(string message, Token token)
        : base($"{message} at line {token.Line}, column {token.Column}")
    {
        Line = token.Line;
        Column = token.Column;
    }
}
