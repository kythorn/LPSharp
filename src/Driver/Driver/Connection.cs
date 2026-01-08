using System.Net.Sockets;
using System.Text;

namespace Driver;

/// <summary>
/// Represents a single client connection with its own isolated REPL context.
/// Each connection has its own Interpreter instance, so variables are not shared.
/// </summary>
public class Connection : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly StreamWriter _writer;
    private readonly StringBuilder _inputBuffer = new();
    private readonly Interpreter _interpreter;
    private readonly string _id;

    private bool _disposed;

    public string Id => _id;
    public bool IsConnected => _client.Connected && !_disposed;

    public Connection(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        _writer = new StreamWriter(_stream, Encoding.ASCII) { AutoFlush = true };
        _id = Guid.NewGuid().ToString()[..8];

        // Each connection gets its own interpreter with output going to this connection
        _interpreter = new Interpreter(_writer);
    }

    /// <summary>
    /// Send a message to this connection.
    /// </summary>
    public void Send(string message)
    {
        if (!IsConnected) return;

        try
        {
            _writer.Write(message);
        }
        catch (IOException)
        {
            // Connection closed
        }
    }

    /// <summary>
    /// Send a line to this connection (appends newline).
    /// </summary>
    public void SendLine(string message)
    {
        Send(message + "\r\n");
    }

    /// <summary>
    /// Process any available input from the connection.
    /// Returns complete lines that have been received.
    /// </summary>
    public List<string> ReadAvailableLines()
    {
        var lines = new List<string>();

        if (!IsConnected) return lines;

        try
        {
            // Read any available data
            while (_stream.DataAvailable)
            {
                var buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    // Connection closed
                    break;
                }

                // Add to input buffer, handling telnet basics
                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];

                    // Skip telnet IAC sequences (255 = IAC)
                    if (b == 255 && i + 2 < bytesRead)
                    {
                        i += 2; // Skip IAC + command + option
                        continue;
                    }

                    // Handle backspace
                    if (b == 8 || b == 127)
                    {
                        if (_inputBuffer.Length > 0)
                        {
                            _inputBuffer.Length--;
                        }
                        continue;
                    }

                    // Handle newline - extract complete line
                    if (b == '\n')
                    {
                        var line = _inputBuffer.ToString().TrimEnd('\r');
                        _inputBuffer.Clear();
                        lines.Add(line);
                        continue;
                    }

                    // Skip carriage return (we handle it with newline)
                    if (b == '\r')
                    {
                        continue;
                    }

                    // Regular character
                    if (b >= 32 && b < 127)
                    {
                        _inputBuffer.Append((char)b);
                    }
                }
            }
        }
        catch (IOException)
        {
            // Connection closed
        }

        return lines;
    }

    /// <summary>
    /// Process a line of input through the REPL.
    /// </summary>
    public void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            SendPrompt();
            return;
        }

        // Handle quit command
        if (line.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            line.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            SendLine("Goodbye!");
            Dispose();
            return;
        }

        try
        {
            var lexer = new Lexer(line);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            var result = _interpreter.Evaluate(ast);

            // Show result (write() already outputs, so only show non-1 results or non-write calls)
            SendLine($"=> {FormatResult(result)}");
        }
        catch (LexerException ex)
        {
            SendLine($"Lexer error: {ex.Message}");
        }
        catch (ParserException ex)
        {
            SendLine($"Parser error: {ex.Message}");
        }
        catch (InterpreterException ex)
        {
            SendLine($"Runtime error: {ex.Message}");
        }
        catch (Exception ex)
        {
            SendLine($"Error: {ex.Message}");
        }

        SendPrompt();
    }

    private static string FormatResult(object result)
    {
        return result switch
        {
            string s => $"\"{s}\"",
            _ => result.ToString() ?? "null"
        };
    }

    public void SendPrompt()
    {
        Send($"[{_id}]> ");
    }

    public void SendWelcome()
    {
        SendLine("Welcome to LPMud Driver");
        SendLine($"Connection ID: {_id}");
        SendLine("Type expressions to evaluate, 'quit' to disconnect.");
        SendLine("");
        SendPrompt();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _writer.Dispose();
            _stream.Dispose();
            _client.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}
