namespace Driver;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

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

    private Token? NextToken()
    {
        SkipWhitespace();

        if (IsAtEnd())
            return null;

        int startColumn = _column;
        char c = Advance();

        if (c == '+')
        {
            return new Token(TokenType.Plus, "+", _line, startColumn);
        }

        if (char.IsDigit(c))
        {
            return ReadNumber(startColumn);
        }

        throw new Exception($"Unexpected character '{c}' at line {_line}, column {startColumn}");
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

    private void SkipWhitespace()
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
            else
            {
                break;
            }
        }
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
}
