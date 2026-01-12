namespace Driver;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["while"] = TokenType.While,
        ["for"] = TokenType.For,
        ["return"] = TokenType.Return,
        ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["switch"] = TokenType.Switch,
        ["case"] = TokenType.Case,
        ["default"] = TokenType.Default,
        ["foreach"] = TokenType.Foreach,
        ["in"] = TokenType.In,
        ["inherit"] = TokenType.Inherit,
        ["int"] = TokenType.Int,
        ["string"] = TokenType.StringType,
        ["object"] = TokenType.Object,
        ["mapping"] = TokenType.Mapping,
        ["mixed"] = TokenType.Mixed,
        ["void"] = TokenType.Void,
        // Visibility modifiers
        ["public"] = TokenType.Public,
        ["private"] = TokenType.Private,
        ["protected"] = TokenType.Protected,
        ["static"] = TokenType.Static,
        ["nomask"] = TokenType.Nomask,
        ["varargs"] = TokenType.Varargs,
        // Error handling
        ["catch"] = TokenType.Catch,
    };

    public Lexer(string source)
    {
        _source = source;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            var token = NextToken();
            if (token != null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token(TokenType.Eof, "", _line, _column));
        return tokens;
    }

    public Token? NextToken()
    {
        SkipWhitespaceAndComments();

        if (IsAtEnd())
            return null;

        int startColumn = _column;
        char c = Advance();

        // Single-character tokens (some may have multi-char variants)
        switch (c)
        {
            case '(':
                // Check for LPC array/mapping literals: ({ and ([
                if (Match('{')) return new Token(TokenType.ArrayStart, "({", _line, startColumn);
                if (Match('[')) return new Token(TokenType.MappingStart, "([", _line, startColumn);
                return new Token(TokenType.LeftParen, "(", _line, startColumn);
            case ')': return new Token(TokenType.RightParen, ")", _line, startColumn);
            case '{': return new Token(TokenType.LeftBrace, "{", _line, startColumn);
            case '}':
                // Check for LPC array/mapping end: })
                if (Match(')')) return new Token(TokenType.ArrayEnd, "})", _line, startColumn);
                return new Token(TokenType.RightBrace, "}", _line, startColumn);
            case '[': return new Token(TokenType.LeftBracket, "[", _line, startColumn);
            case ']':
                // Check for LPC mapping end: ])
                if (Match(')')) return new Token(TokenType.MappingEnd, "])", _line, startColumn);
                return new Token(TokenType.RightBracket, "]", _line, startColumn);
            case ';': return new Token(TokenType.Semicolon, ";", _line, startColumn);
            case ',': return new Token(TokenType.Comma, ",", _line, startColumn);
            case '~': return new Token(TokenType.Tilde, "~", _line, startColumn);
            case '?': return new Token(TokenType.Question, "?", _line, startColumn);
        }

        // Multi-character operators
        switch (c)
        {
            case '+':
                if (Match('+')) return new Token(TokenType.PlusPlus, "++", _line, startColumn);
                if (Match('=')) return new Token(TokenType.PlusEqual, "+=", _line, startColumn);
                return new Token(TokenType.Plus, "+", _line, startColumn);

            case '-':
                if (Match('-')) return new Token(TokenType.MinusMinus, "--", _line, startColumn);
                if (Match('=')) return new Token(TokenType.MinusEqual, "-=", _line, startColumn);
                if (Match('>')) return new Token(TokenType.Arrow, "->", _line, startColumn);
                return new Token(TokenType.Minus, "-", _line, startColumn);

            case '*':
                if (Match('=')) return new Token(TokenType.StarEqual, "*=", _line, startColumn);
                return new Token(TokenType.Star, "*", _line, startColumn);

            case '/':
                if (Match('=')) return new Token(TokenType.SlashEqual, "/=", _line, startColumn);
                return new Token(TokenType.Slash, "/", _line, startColumn);

            case '%':
                if (Match('=')) return new Token(TokenType.PercentEqual, "%=", _line, startColumn);
                return new Token(TokenType.Percent, "%", _line, startColumn);

            case '=':
                if (Match('=')) return new Token(TokenType.EqualEqual, "==", _line, startColumn);
                return new Token(TokenType.Equal, "=", _line, startColumn);

            case '!':
                if (Match('=')) return new Token(TokenType.BangEqual, "!=", _line, startColumn);
                return new Token(TokenType.Bang, "!", _line, startColumn);

            case '<':
                if (Match('<'))
                {
                    if (Match('=')) return new Token(TokenType.LessLessEqual, "<<=", _line, startColumn);
                    return new Token(TokenType.LessLess, "<<", _line, startColumn);
                }
                if (Match('=')) return new Token(TokenType.LessEqual, "<=", _line, startColumn);
                return new Token(TokenType.Less, "<", _line, startColumn);

            case '>':
                if (Match('>'))
                {
                    if (Match('=')) return new Token(TokenType.GreaterGreaterEqual, ">>=", _line, startColumn);
                    return new Token(TokenType.GreaterGreater, ">>", _line, startColumn);
                }
                if (Match('=')) return new Token(TokenType.GreaterEqual, ">=", _line, startColumn);
                return new Token(TokenType.Greater, ">", _line, startColumn);

            case '&':
                if (Match('&')) return new Token(TokenType.AmpAmp, "&&", _line, startColumn);
                if (Match('=')) return new Token(TokenType.AmpEqual, "&=", _line, startColumn);
                return new Token(TokenType.Amp, "&", _line, startColumn);

            case '|':
                if (Match('|')) return new Token(TokenType.PipePipe, "||", _line, startColumn);
                if (Match('=')) return new Token(TokenType.PipeEqual, "|=", _line, startColumn);
                return new Token(TokenType.Pipe, "|", _line, startColumn);

            case '^':
                if (Match('=')) return new Token(TokenType.CaretEqual, "^=", _line, startColumn);
                return new Token(TokenType.Caret, "^", _line, startColumn);

            case ':':
                if (Match(':')) return new Token(TokenType.ColonColon, "::", _line, startColumn);
                return new Token(TokenType.Colon, ":", _line, startColumn);
        }

        // String literals
        if (c == '"')
        {
            return ReadString(startColumn);
        }

        // Numbers
        if (char.IsDigit(c))
        {
            return ReadNumber(startColumn);
        }

        // Identifiers and keywords
        if (IsIdentifierStart(c))
        {
            return ReadIdentifier(startColumn);
        }

        throw new LexerException($"Unexpected character '{c}'", _line, startColumn);
    }

    private Token ReadNumber(int startColumn)
    {
        int start = _position - 1;

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            Advance();
        }

        string lexeme = _source[start.._position];
        return new Token(TokenType.Number, lexeme, _line, startColumn);
    }

    private Token ReadString(int startColumn)
    {
        int startLine = _line;
        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            char c = Peek();
            if (c == '\n')
            {
                throw new LexerException("Unterminated string (newline in string)", startLine, startColumn);
            }
            if (c == '\\')
            {
                Advance(); // consume backslash
                if (IsAtEnd())
                {
                    throw new LexerException("Unterminated string (escape at end)", startLine, startColumn);
                }
                char escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    '0' => '\0',
                    _ => throw new LexerException($"Invalid escape sequence '\\{escaped}'", _line, _column - 1)
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
        {
            throw new LexerException("Unterminated string", startLine, startColumn);
        }

        Advance(); // consume closing quote
        return new Token(TokenType.String, sb.ToString(), startLine, startColumn);
    }

    private Token ReadIdentifier(int startColumn)
    {
        int start = _position - 1;

        while (!IsAtEnd() && IsIdentifierChar(Peek()))
        {
            Advance();
        }

        string lexeme = _source[start.._position];

        if (Keywords.TryGetValue(lexeme, out var keywordType))
        {
            return new Token(keywordType, lexeme, _line, startColumn);
        }

        return new Token(TokenType.Identifier, lexeme, _line, startColumn);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            char c = Peek();

            if (c == ' ' || c == '\t' || c == '\r')
            {
                Advance();
            }
            else if (c == '\n')
            {
                Advance();
                _line++;
                _column = 1;
            }
            else if (c == '/' && _position + 1 < _source.Length)
            {
                char next = _source[_position + 1];
                if (next == '/')
                {
                    // Single-line comment
                    Advance(); // consume first /
                    Advance(); // consume second /
                    while (!IsAtEnd() && Peek() != '\n')
                    {
                        Advance();
                    }
                }
                else if (next == '*')
                {
                    // Multi-line comment
                    Advance(); // consume /
                    Advance(); // consume *
                    SkipBlockComment();
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    private void SkipBlockComment()
    {
        int startLine = _line;
        int startColumn = _column - 2; // we already consumed /*

        while (!IsAtEnd())
        {
            if (Peek() == '*' && _position + 1 < _source.Length && _source[_position + 1] == '/')
            {
                Advance(); // consume *
                Advance(); // consume /
                return;
            }
            if (Peek() == '\n')
            {
                Advance();
                _line++;
                _column = 1;
            }
            else
            {
                Advance();
            }
        }

        throw new LexerException("Unterminated block comment", startLine, startColumn);
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected)
        {
            return false;
        }
        _position++;
        _column++;
        return true;
    }

    private char Peek()
    {
        return _source[_position];
    }

    private char Advance()
    {
        _column++;
        return _source[_position++];
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}

public class LexerException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public LexerException(string message, int line, int column)
        : base($"{message} at line {line}, column {column}")
    {
        Line = line;
        Column = column;
    }
}
