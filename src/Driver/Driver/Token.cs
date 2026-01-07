namespace Driver;

public enum TokenType
{
    // Literals
    Number,

    // Operators
    Plus,

    // Special
    Eof,
}

public record Token(TokenType Type, string Lexeme, int Line, int Column)
{
    public override string ToString() => Type switch
    {
        TokenType.Number => $"NUMBER({Lexeme})",
        TokenType.Plus => "PLUS",
        TokenType.Eof => "EOF",
        _ => Type.ToString()
    };
}
