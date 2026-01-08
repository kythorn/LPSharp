using Driver;

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

try
{
    return args[0] switch
    {
        "--tokenize" => Tokenize(args),
        "--eval" => Eval(args),
        "--repl" => Repl(),
        "--server" => Server(args),
        "--help" or "-h" => PrintUsage(),
        _ => UnknownCommand(args[0])
    };
}
catch (LexerException ex)
{
    Console.Error.WriteLine($"Lexer error: {ex.Message}");
    return 1;
}
catch (ParserException ex)
{
    Console.Error.WriteLine($"Parse error: {ex.Message}");
    return 1;
}
catch (InterpreterException ex)
{
    Console.Error.WriteLine($"Runtime error: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

int PrintUsage()
{
    Console.WriteLine("""
        LPSharp - LPC Language Driver

        Usage:
          driver --tokenize <file>     Tokenize an LPC file and print tokens
          driver --eval "<expression>" Evaluate an LPC expression
          driver --repl                Start interactive REPL
          driver --server [port]       Start telnet server (default port: 4000)
          driver --help                Show this help message

        Examples:
          driver --tokenize test.c
          driver --eval "5 + 3 * 2"
          driver --server 4000
        """);
    return 0;
}

int Tokenize(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: --tokenize requires a file path");
        return 1;
    }

    string filePath = args[1];
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"Error: File not found: {filePath}");
        return 1;
    }

    string source = File.ReadAllText(filePath);
    var lexer = new Lexer(source);
    var tokens = lexer.Tokenize();

    foreach (var token in tokens)
    {
        Console.WriteLine($"[{token.Line}:{token.Column}] {token}");
    }

    Console.WriteLine($"\nTotal: {tokens.Count} tokens");
    return 0;
}

int Eval(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: --eval requires an expression");
        return 1;
    }

    string expression = args[1];
    var lexer = new Lexer(expression);
    var tokens = lexer.Tokenize();
    var parser = new Parser(tokens);
    var ast = parser.Parse();
    var interpreter = new Interpreter();
    var result = interpreter.Evaluate(ast);

    Console.WriteLine(result);
    return 0;
}

int Repl()
{
    Console.WriteLine("LPSharp REPL - Type expressions to evaluate, 'quit' to exit");
    var interpreter = new Interpreter();

    while (true)
    {
        Console.Write("LPC> ");
        var line = Console.ReadLine();

        if (line == null || line == "quit")
        {
            break;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        try
        {
            var lexer = new Lexer(line);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            var result = interpreter.Evaluate(ast);
            Console.WriteLine(result);
        }
        catch (LexerException ex)
        {
            Console.Error.WriteLine($"Lexer error: {ex.Message}");
        }
        catch (ParserException ex)
        {
            Console.Error.WriteLine($"Parse error: {ex.Message}");
        }
        catch (InterpreterException ex)
        {
            Console.Error.WriteLine($"Runtime error: {ex.Message}");
        }
    }

    return 0;
}

int Server(string[] args)
{
    int port = 4000; // Default port

    if (args.Length >= 2)
    {
        if (!int.TryParse(args[1], out port) || port < 1 || port > 65535)
        {
            Console.Error.WriteLine($"Error: Invalid port number: {args[1]}");
            return 1;
        }
    }

    using var server = new TelnetServer(port);
    server.Run();
    return 0;
}

int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Use --help for usage information");
    return 1;
}
