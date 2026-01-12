namespace Driver;

public enum TokenType
{
    // Literals
    Number,
    String,

    // Identifiers
    Identifier,

    // Keywords
    If,
    Else,
    While,
    For,
    Return,
    Break,
    Continue,
    Switch,
    Case,
    Default,
    Foreach,
    In,
    Inherit,
    Int,
    StringType,
    Object,
    Mapping,
    Mixed,
    Void,

    // Visibility modifiers
    Public,
    Private,
    Protected,
    Static,
    Nomask,
    Varargs,

    // Error handling
    Catch,

    // Arithmetic operators
    Plus,           // +
    Minus,          // -
    Star,           // *
    Slash,          // /
    Percent,        // %

    // Comparison operators
    EqualEqual,     // ==
    BangEqual,      // !=
    Less,           // <
    Greater,        // >
    LessEqual,      // <=
    GreaterEqual,   // >=

    // Logical operators
    AmpAmp,         // &&
    PipePipe,       // ||
    Bang,           // !

    // Bitwise operators
    Amp,            // &
    Pipe,           // |
    Caret,          // ^
    Tilde,          // ~
    LessLess,       // <<
    GreaterGreater, // >>

    // Assignment operators
    Equal,          // =
    PlusEqual,      // +=
    MinusEqual,     // -=
    StarEqual,      // *=
    SlashEqual,     // /=
    PercentEqual,   // %=
    AmpEqual,       // &=
    PipeEqual,      // |=
    CaretEqual,     // ^=
    LessLessEqual,  // <<=
    GreaterGreaterEqual, // >>=

    // Increment/Decrement
    PlusPlus,       // ++
    MinusMinus,     // --

    // Other operators
    Question,       // ?
    Colon,          // :
    ColonColon,     // ::
    Arrow,          // ->

    // Delimiters
    LeftParen,      // (
    RightParen,     // )
    LeftBrace,      // {
    RightBrace,     // }
    LeftBracket,    // [
    RightBracket,   // ]
    Semicolon,      // ;
    Comma,          // ,

    // LPC special delimiters
    ArrayStart,     // ({
    ArrayEnd,       // })
    MappingStart,   // ([
    MappingEnd,     // ])

    // Special
    Eof,
}

public record Token(TokenType Type, string Lexeme, int Line, int Column)
{
    public override string ToString() => Type switch
    {
        TokenType.Number => $"NUMBER({Lexeme})",
        TokenType.String => $"STRING({Lexeme})",
        TokenType.Identifier => $"IDENTIFIER({Lexeme})",
        _ => Type.ToString().ToUpper()
    };
}
