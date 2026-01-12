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
          driver --server [options]    Start telnet server
          driver --help                Show this help message

        Server options:
          --port <port>                Port number (default: 4000)
          --mudlib <path>              Mudlib directory (default: ./mudlib)

        Examples:
          driver --tokenize test.c
          driver --eval "5 + 3 * 2"
          driver --server
          driver --server --port 4000 --mudlib ./mudlib
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
        Console.Error.WriteLine("Error: --eval requires an expression or statement");
        return 1;
    }

    string input = args[1];
    var lexer = new Lexer(input);
    var tokens = lexer.Tokenize();
    var parser = new Parser(tokens);
    var parsed = parser.ParseStatementOrExpression();
    var interpreter = new Interpreter();

    object? result;
    if (parsed is Statement stmt)
    {
        try
        {
            result = interpreter.Execute(stmt);
        }
        catch (ReturnException ret)
        {
            result = ret.Value;
        }
    }
    else if (parsed is Expression expr)
    {
        result = interpreter.Evaluate(expr);
    }
    else
    {
        result = null;
    }

    if (result != null)
    {
        Console.WriteLine(result);
    }
    return 0;
}

int Repl()
{
    Console.WriteLine("LPSharp REPL - Type expressions or statements, 'quit' to exit");
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
            var parsed = parser.ParseStatementOrExpression();

            object? result;
            if (parsed is Statement stmt)
            {
                result = interpreter.Execute(stmt);
            }
            else if (parsed is Expression expr)
            {
                result = interpreter.Evaluate(expr);
            }
            else
            {
                result = null;
            }

            if (result != null)
            {
                Console.WriteLine(result);
            }
        }
        catch (ReturnException ret)
        {
            if (ret.Value != null)
            {
                Console.WriteLine(ret.Value);
            }
        }
        catch (BreakException)
        {
            Console.Error.WriteLine("Error: break outside of loop");
        }
        catch (ContinueException)
        {
            Console.Error.WriteLine("Error: continue outside of loop");
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
    string mudlibPath = "./mudlib"; // Default mudlib path

    // Parse arguments
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--mudlib" && i + 1 < args.Length)
        {
            mudlibPath = args[++i];
        }
        else if (args[i] == "--port" && i + 1 < args.Length)
        {
            if (!int.TryParse(args[++i], out port) || port < 1 || port > 65535)
            {
                Console.Error.WriteLine($"Error: Invalid port number: {args[i]}");
                return 1;
            }
        }
        else if (int.TryParse(args[i], out var parsedPort) && parsedPort >= 1 && parsedPort <= 65535)
        {
            port = parsedPort;
        }
    }

    // Check mudlib path exists
    if (!Directory.Exists(mudlibPath))
    {
        Console.Error.WriteLine($"Error: Mudlib directory not found: {mudlibPath}");
        return 1;
    }

    Console.WriteLine($"Starting LPMud Revival...");
    Console.WriteLine($"  Mudlib: {Path.GetFullPath(mudlibPath)}");
    Console.WriteLine($"  Port: {port}");
    Console.WriteLine();

    // Create object manager
    var objectManager = new ObjectManager(mudlibPath);
    objectManager.InitializeInterpreter();

    // Create account manager
    var accountManager = new AccountManager(mudlibPath);

    // Create game loop
    var gameLoop = new GameLoop(objectManager, accountManager);

    // Get the interpreter from ObjectManager and pass it to GameLoop
    // We need to access it via reflection or add a property
    // For now, let's create our own interpreter instance
    var interpreter = new ObjectInterpreter(objectManager);
    gameLoop.InitializeInterpreter(interpreter);

    // Start game loop
    gameLoop.Start();

    // Create and start telnet server
    using var server = new TelnetServer(port, gameLoop);
    try
    {
        server.Run();
    }
    finally
    {
        // Cleanup
        gameLoop.Stop();
    }

    return 0;
}

int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Use --help for usage information");
    return 1;
}
