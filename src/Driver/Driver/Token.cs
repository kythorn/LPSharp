namespace Driver;

public enum TokenType
{
    // Literals
    Number,

    // Operators
    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    // Special
    Eof,
}

public record Token(TokenType Type, string Lexeme, int Line, int Column)
{
    public override string ToString() => Type switch
    {
        TokenType.Number => $"NUMBER({Lexeme})",
        TokenType.Plus => "PLUS",
        TokenType.Minus => "MINUS",
        TokenType.Star => "STAR",
        TokenType.Slash => "SLASH",
        TokenType.Percent => "PERCENT",
        TokenType.Eof => "EOF",
        _ => Type.ToString()
    };
}
