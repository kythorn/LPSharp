using System.Net.Sockets;
using System.Text;

namespace Driver;

/// <summary>
/// Represents a single client connection.
/// Commands are queued to the GameLoop for processing.
/// </summary>
public class Connection : IDisposable
{
    // Telnet protocol constants
    private const byte IAC = 255;   // Interpret As Command
    private const byte WILL = 251;  // Will do option
    private const byte WONT = 252;  // Won't do option
    private const byte ECHO = 1;    // Echo option

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly StreamWriter _writer;
    private readonly StringBuilder _inputBuffer = new();
    private readonly GameLoop _gameLoop;
    private readonly string _id;

    private bool _disposed;

    public string Id => _id;
    public bool IsConnected => _client.Connected && !_disposed;

    public Connection(TcpClient client, GameLoop gameLoop)
    {
        _client = client;
        _gameLoop = gameLoop;
        _stream = client.GetStream();
        _writer = new StreamWriter(_stream, Encoding.ASCII) { AutoFlush = true };
        _id = Guid.NewGuid().ToString()[..8];
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
    /// Control local echo on the client side.
    /// When disabled, typed characters won't be echoed (for password input).
    /// </summary>
    /// <param name="enabled">True to enable echo, false to suppress.</param>
    public void SetEchoMode(bool enabled)
    {
        if (!IsConnected) return;

        try
        {
            // IAC WILL ECHO = server will handle echo (client should not echo)
            // IAC WONT ECHO = server won't handle echo (client should echo)
            byte[] command = enabled
                ? new byte[] { IAC, WONT, ECHO }
                : new byte[] { IAC, WILL, ECHO };

            _stream.Write(command, 0, command.Length);
            _stream.Flush();
        }
        catch (IOException)
        {
            // Connection closed
        }
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
    /// Process a line of input by queuing it to the game loop.
    /// </summary>
    public void ProcessLine(string line)
    {
        // Queue the command to the game loop for processing
        _gameLoop.QueueCommand(_id, line);
    }

    public void SendWelcome()
    {
        SendLine("Welcome to LPMud Revival!");
        SendLine("");
        Send("> ");
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
