namespace Driver;

/// <summary>
/// Recursive descent parser for LPC expressions and statements.
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

    /// <summary>
    /// Parse a single expression (original behavior for REPL).
    /// </summary>
    public Expression Parse()
    {
        var expr = ParseExpression();

        if (!IsAtEnd() && Current().Type != TokenType.Eof)
        {
            throw new ParserException($"Unexpected token '{Current().Lexeme}'", Current());
        }

        return expr;
    }

    /// <summary>
    /// Parse a complete LPC program file (top-level declarations).
    /// Returns a list of statements including inherits, variables, and functions.
    /// </summary>
    public List<Statement> ParseProgram()
    {
        var statements = new List<Statement>();

        while (!IsAtEnd() && Current().Type != TokenType.Eof)
        {
            // Parse top-level declarations
            if (Check(TokenType.Inherit))
            {
                statements.Add(ParseInheritStatement());
            }
            else if (IsFunctionDefinitionStart())
            {
                statements.Add(ParseFunctionDefinition());
            }
            else if (IsTypeName(Current().Type))
            {
                // Could be variable declaration
                statements.Add(ParseVariableDeclaration());
            }
            else
            {
                throw new ParserException($"Unexpected token at top level: '{Current().Lexeme}'", Current());
            }
        }

        return statements;
    }

    /// <summary>
    /// Parse input as either a statement or expression.
    /// If it looks like a statement (starts with keyword or {), parse as statement.
    /// Otherwise parse as expression.
    /// </summary>
    public object ParseStatementOrExpression()
    {
        // Check if this looks like a statement
        if (IsStatementStart())
        {
            var stmt = ParseStatement();

            if (!IsAtEnd() && Current().Type != TokenType.Eof)
            {
                throw new ParserException($"Unexpected token '{Current().Lexeme}'", Current());
            }

            return stmt;
        }

        // Parse as expression
        var expr = ParseExpression();

        // If followed by semicolon, it's an expression statement
        if (Match(TokenType.Semicolon))
        {
            if (!IsAtEnd() && Current().Type != TokenType.Eof)
            {
                throw new ParserException($"Unexpected token '{Current().Lexeme}'", Current());
            }

            return new ExpressionStatement(expr)
            {
                Line = expr.Line,
                Column = expr.Column
            };
        }

        if (!IsAtEnd() && Current().Type != TokenType.Eof)
        {
            throw new ParserException($"Unexpected token '{Current().Lexeme}'", Current());
        }

        return expr;
    }

    private bool IsStatementStart()
    {
        // Check for function definition: type identifier(
        if (IsFunctionDefinitionStart())
        {
            return true;
        }

        return Current().Type is TokenType.If or TokenType.While or TokenType.For
            or TokenType.Return or TokenType.Break or TokenType.Continue
            or TokenType.LeftBrace;
    }

    private bool IsFunctionDefinitionStart()
    {
        // Function definition pattern: type identifier(
        // Type is either a keyword (int, string, void, etc.) or an identifier
        if (!IsTypeName(Current().Type))
        {
            return false;
        }

        // Look ahead for identifier followed by (
        if (_position + 2 >= _tokens.Count)
        {
            return false;
        }

        var second = _tokens[_position + 1];
        var third = _tokens[_position + 2];

        return second.Type == TokenType.Identifier && third.Type == TokenType.LeftParen;
    }

    private static bool IsTypeName(TokenType type)
    {
        // LPC type keywords
        return type is TokenType.Int or TokenType.StringType or TokenType.Void
            or TokenType.Object or TokenType.Mixed or TokenType.Mapping
            or TokenType.Identifier; // for user-defined types
    }

    /// <summary>
    /// Check if we're looking at a local variable declaration.
    /// Pattern: type identifier (not followed by '(' which would be a function)
    /// </summary>
    private bool IsLocalVariableDeclaration()
    {
        // Need at least 2 tokens ahead
        if (_position + 1 >= _tokens.Count)
        {
            return false;
        }

        var current = Current();
        var next = _tokens[_position + 1];

        // Must start with a type keyword (not just any identifier, to avoid ambiguity)
        // We check for actual type keywords, not identifiers which could be function calls
        bool isTypeKeyword = current.Type is TokenType.Int or TokenType.StringType
            or TokenType.Void or TokenType.Object or TokenType.Mixed or TokenType.Mapping;

        if (!isTypeKeyword)
        {
            return false;
        }

        // Next must be an identifier (the variable name)
        if (next.Type != TokenType.Identifier)
        {
            return false;
        }

        // If there's a third token, make sure it's not '(' (which would make this a function)
        if (_position + 2 < _tokens.Count)
        {
            var third = _tokens[_position + 2];
            if (third.Type == TokenType.LeftParen)
            {
                return false; // This is a function definition, not a variable
            }
        }

        return true;
    }

    #region Statement Parsing

    private Statement ParseStatement()
    {
        // Check for function definition before other statements
        if (IsFunctionDefinitionStart())
        {
            return ParseFunctionDefinition();
        }

        if (Match(TokenType.If)) return ParseIfStatement();
        if (Match(TokenType.While)) return ParseWhileStatement();
        if (Match(TokenType.For)) return ParseForStatement();
        if (Match(TokenType.Return)) return ParseReturnStatement();
        if (Match(TokenType.Break)) return ParseBreakStatement();
        if (Match(TokenType.Continue)) return ParseContinueStatement();
        if (Match(TokenType.LeftBrace)) return ParseBlockStatement();

        // Check for local variable declaration (type identifier ...)
        if (IsLocalVariableDeclaration())
        {
            return ParseVariableDeclaration();
        }

        // Expression statement
        return ParseExpressionStatement();
    }

    private FunctionDefinition ParseFunctionDefinition()
    {
        var startToken = Current();

        // Capture return type
        var returnType = Current().Lexeme;
        Advance();

        // Function name
        if (!Match(TokenType.Identifier))
        {
            throw new ParserException("Expected function name", Current());
        }
        var name = Previous().Lexeme;

        // Parameter list
        if (!Match(TokenType.LeftParen))
        {
            throw new ParserException("Expected '(' after function name", Current());
        }

        var parameters = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                // Skip parameter type if present
                if (IsTypeName(Current().Type) && _position + 1 < _tokens.Count
                    && _tokens[_position + 1].Type == TokenType.Identifier)
                {
                    Advance(); // Skip type
                }

                if (!Match(TokenType.Identifier))
                {
                    throw new ParserException("Expected parameter name", Current());
                }
                parameters.Add(Previous().Lexeme);
            } while (Match(TokenType.Comma));
        }

        if (!Match(TokenType.RightParen))
        {
            throw new ParserException("Expected ')' after parameters", Current());
        }

        // Function body
        if (!Match(TokenType.LeftBrace))
        {
            throw new ParserException("Expected '{' before function body", Current());
        }
        var body = ParseBlockStatement();

        return new FunctionDefinition(returnType, name, parameters, body)
        {
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private InheritStatement ParseInheritStatement()
    {
        var inheritToken = Current();
        Advance(); // consume 'inherit'

        if (!Match(TokenType.String))
        {
            throw new ParserException("Expected string path after 'inherit'", Current());
        }
        var path = Previous().Lexeme;

        // Remove quotes from path
        if (path.StartsWith('"') && path.EndsWith('"'))
        {
            path = path[1..^1];
        }

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after inherit statement", Current());
        }

        return new InheritStatement(path)
        {
            Line = inheritToken.Line,
            Column = inheritToken.Column
        };
    }

    private VariableDeclaration ParseVariableDeclaration()
    {
        var typeToken = Current();
        var type = typeToken.Lexeme;
        Advance(); // consume type

        if (!Match(TokenType.Identifier))
        {
            throw new ParserException("Expected variable name after type", Current());
        }
        var name = Previous().Lexeme;

        Expression? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = ParseExpression();
        }

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after variable declaration", Current());
        }

        return new VariableDeclaration(type, name, initializer)
        {
            Line = typeToken.Line,
            Column = typeToken.Column
        };
    }

    private BlockStatement ParseBlockStatement()
    {
        var openBrace = Previous();
        var statements = new List<Statement>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            statements.Add(ParseStatement());
        }

        if (!Match(TokenType.RightBrace))
        {
            throw new ParserException("Expected '}' after block", Current());
        }

        return new BlockStatement(statements)
        {
            Line = openBrace.Line,
            Column = openBrace.Column
        };
    }

    private IfStatement ParseIfStatement()
    {
        var ifToken = Previous();

        if (!Match(TokenType.LeftParen))
        {
            throw new ParserException("Expected '(' after 'if'", Current());
        }

        var condition = ParseExpression();

        if (!Match(TokenType.RightParen))
        {
            throw new ParserException("Expected ')' after if condition", Current());
        }

        var thenBranch = ParseStatement();
        Statement? elseBranch = null;

        if (Match(TokenType.Else))
        {
            elseBranch = ParseStatement();
        }

        return new IfStatement(condition, thenBranch, elseBranch)
        {
            Line = ifToken.Line,
            Column = ifToken.Column
        };
    }

    private WhileStatement ParseWhileStatement()
    {
        var whileToken = Previous();

        if (!Match(TokenType.LeftParen))
        {
            throw new ParserException("Expected '(' after 'while'", Current());
        }

        var condition = ParseExpression();

        if (!Match(TokenType.RightParen))
        {
            throw new ParserException("Expected ')' after while condition", Current());
        }

        var body = ParseStatement();

        return new WhileStatement(condition, body)
        {
            Line = whileToken.Line,
            Column = whileToken.Column
        };
    }

    private ForStatement ParseForStatement()
    {
        var forToken = Previous();

        if (!Match(TokenType.LeftParen))
        {
            throw new ParserException("Expected '(' after 'for'", Current());
        }

        // Init expression (optional)
        Expression? init = null;
        if (!Check(TokenType.Semicolon))
        {
            init = ParseExpression();
        }
        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after for initializer", Current());
        }

        // Condition expression (optional)
        Expression? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = ParseExpression();
        }
        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after for condition", Current());
        }

        // Increment expression (optional)
        Expression? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = ParseExpression();
        }
        if (!Match(TokenType.RightParen))
        {
            throw new ParserException("Expected ')' after for clauses", Current());
        }

        var body = ParseStatement();

        return new ForStatement(init, condition, increment, body)
        {
            Line = forToken.Line,
            Column = forToken.Column
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        var returnToken = Previous();
        Expression? value = null;

        if (!Check(TokenType.Semicolon))
        {
            value = ParseExpression();
        }

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after return statement", Current());
        }

        return new ReturnStatement(value)
        {
            Line = returnToken.Line,
            Column = returnToken.Column
        };
    }

    private BreakStatement ParseBreakStatement()
    {
        var breakToken = Previous();

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after 'break'", Current());
        }

        return new BreakStatement
        {
            Line = breakToken.Line,
            Column = breakToken.Column
        };
    }

    private ContinueStatement ParseContinueStatement()
    {
        var continueToken = Previous();

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after 'continue'", Current());
        }

        return new ContinueStatement
        {
            Line = continueToken.Line,
            Column = continueToken.Column
        };
    }

    private ExpressionStatement ParseExpressionStatement()
    {
        var expr = ParseExpression();

        if (!Match(TokenType.Semicolon))
        {
            throw new ParserException("Expected ';' after expression", Current());
        }

        return new ExpressionStatement(expr)
        {
            Line = expr.Line,
            Column = expr.Column
        };
    }

    #endregion

    #region Expression Parsing

    private Expression ParseExpression()
    {
        return ParseAssignment();
    }

    // Precedence 15: Assignment (right-to-left)
    private Expression ParseAssignment()
    {
        var expr = ParseTernary();

        // Simple assignment
        if (Match(TokenType.Equal))
        {
            var equals = Previous();

            // Left side must be an identifier for simple assignment
            if (expr is Identifier id)
            {
                var value = ParseAssignment(); // Right-to-left associativity
                return new Assignment(id.Name, value)
                {
                    Line = equals.Line,
                    Column = equals.Column
                };
            }

            throw new ParserException("Invalid assignment target", equals);
        }

        // Compound assignment operators
        if (Match(TokenType.PlusEqual, TokenType.MinusEqual, TokenType.StarEqual,
                  TokenType.SlashEqual, TokenType.PercentEqual, TokenType.AmpEqual,
                  TokenType.PipeEqual, TokenType.CaretEqual, TokenType.LessLessEqual,
                  TokenType.GreaterGreaterEqual))
        {
            var op = Previous();

            if (expr is Identifier id)
            {
                var binOp = op.Type switch
                {
                    TokenType.PlusEqual => BinaryOperator.Add,
                    TokenType.MinusEqual => BinaryOperator.Subtract,
                    TokenType.StarEqual => BinaryOperator.Multiply,
                    TokenType.SlashEqual => BinaryOperator.Divide,
                    TokenType.PercentEqual => BinaryOperator.Modulo,
                    TokenType.AmpEqual => BinaryOperator.BitwiseAnd,
                    TokenType.PipeEqual => BinaryOperator.BitwiseOr,
                    TokenType.CaretEqual => BinaryOperator.BitwiseXor,
                    TokenType.LessLessEqual => BinaryOperator.LeftShift,
                    TokenType.GreaterGreaterEqual => BinaryOperator.RightShift,
                    _ => throw new ParserException($"Unexpected compound assignment operator", op)
                };

                var value = ParseAssignment(); // Right-to-left associativity
                return new CompoundAssignment(id.Name, binOp, value)
                {
                    Line = op.Line,
                    Column = op.Column
                };
            }

            throw new ParserException("Invalid assignment target", op);
        }

        return expr;
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

    // Precedence 3: Unary prefix operators: - ! ~ ++ --
    private Expression ParseUnary()
    {
        // Standard unary operators
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

        // Prefix increment/decrement
        if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
        {
            var op = Previous();
            var unaryOp = op.Type == TokenType.PlusPlus
                ? UnaryOperator.PreIncrement
                : UnaryOperator.PreDecrement;
            var operand = ParseUnary(); // Right-to-left associativity

            // Operand must be an identifier
            if (operand is not Identifier)
            {
                throw new ParserException("Increment/decrement requires a variable", op);
            }

            return new UnaryOp(unaryOp, operand)
            {
                Line = op.Line,
                Column = op.Column
            };
        }

        return ParsePostfix();
    }

    // Precedence 2: Postfix operators: ++ -- function calls, and indexing
    private Expression ParsePostfix()
    {
        var expr = ParsePrimary();

        // Check for function call: identifier followed by (
        if (expr is Identifier id && Match(TokenType.LeftParen))
        {
            var openParen = Previous();
            var arguments = new List<Expression>();

            // Parse arguments (comma-separated)
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }

            if (!Match(TokenType.RightParen))
            {
                throw new ParserException("Expected ')' after function arguments", Current());
            }

            expr = new FunctionCall(id.Name, arguments)
            {
                Line = openParen.Line,
                Column = openParen.Column
            };
        }

        // Handle chained postfix operations: indexing and ++/--
        while (true)
        {
            // Check for index expression: expr[index]
            if (Match(TokenType.LeftBracket))
            {
                var bracket = Previous();
                var index = ParseExpression();

                if (!Match(TokenType.RightBracket))
                {
                    throw new ParserException("Expected ']' after index expression", Current());
                }

                expr = new IndexExpression(expr, index)
                {
                    Line = bracket.Line,
                    Column = bracket.Column
                };
                continue;
            }

            // Check for postfix ++/--
            if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
            {
                var op = Previous();

                // Operand must be an identifier
                if (expr is not Identifier)
                {
                    throw new ParserException("Increment/decrement requires a variable", op);
                }

                var unaryOp = op.Type == TokenType.PlusPlus
                    ? UnaryOperator.PostIncrement
                    : UnaryOperator.PostDecrement;

                expr = new UnaryOp(unaryOp, expr, IsPrefix: false)
                {
                    Line = op.Line,
                    Column = op.Column
                };
                // Don't continue - ++/-- can only appear once
            }

            break;
        }

        return expr;
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

        // Parent function call: ::function()
        if (Match(TokenType.ColonColon))
        {
            var colonColon = Previous();

            if (!Match(TokenType.Identifier))
            {
                throw new ParserException("Expected function name after '::'", Current());
            }
            var functionName = Previous().Lexeme;

            if (!Match(TokenType.LeftParen))
            {
                throw new ParserException("Expected '(' after parent function name", Current());
            }

            var arguments = new List<Expression>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }

            if (!Match(TokenType.RightParen))
            {
                throw new ParserException("Expected ')' after parent function arguments", Current());
            }

            return new FunctionCall(functionName, arguments, IsParentCall: true)
            {
                Line = colonColon.Line,
                Column = colonColon.Column
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

        // Array literal: ({ expr, expr, ... })
        if (Match(TokenType.ArrayStart))
        {
            var arrayStart = Previous();
            var elements = new List<Expression>();

            if (!Check(TokenType.ArrayEnd))
            {
                do
                {
                    elements.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }

            if (!Match(TokenType.ArrayEnd))
            {
                throw new ParserException("Expected '})' after array elements", Current());
            }

            return new ArrayLiteral(elements)
            {
                Line = arrayStart.Line,
                Column = arrayStart.Column
            };
        }

        // Mapping literal: ([ key: val, key: val, ... ])
        if (Match(TokenType.MappingStart))
        {
            var mappingStart = Previous();
            var entries = new List<(Expression Key, Expression Value)>();

            if (!Check(TokenType.MappingEnd))
            {
                do
                {
                    var key = ParseExpression();
                    if (!Match(TokenType.Colon))
                    {
                        throw new ParserException("Expected ':' after mapping key", Current());
                    }
                    var value = ParseExpression();
                    entries.Add((key, value));
                } while (Match(TokenType.Comma));
            }

            if (!Match(TokenType.MappingEnd))
            {
                throw new ParserException("Expected '])' after mapping entries", Current());
            }

            return new MappingLiteral(entries)
            {
                Line = mappingStart.Line,
                Column = mappingStart.Column
            };
        }

        // Identifier (variable reference)
        if (Match(TokenType.Identifier))
        {
            var token = Previous();
            return new Identifier(token.Lexeme)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        throw new ParserException($"Expected expression, got '{Current().Lexeme}'", Current());
    }

    #endregion

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
