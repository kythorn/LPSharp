using System.Net;
using System.Net.Sockets;

namespace Driver;

/// <summary>
/// Simple telnet server that accepts multiple concurrent connections.
/// Each connection gets its own isolated REPL context.
/// </summary>
public class TelnetServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly List<Connection> _connections = new();
    private readonly int _port;
    private readonly object _lock = new();

    private bool _running;
    private bool _disposed;

    public int Port => _port;
    public int ConnectionCount
    {
        get
        {
            lock (_lock)
            {
                return _connections.Count;
            }
        }
    }

    public TelnetServer(int port)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>
    /// Start the server and run the main loop.
    /// This blocks until Stop() is called.
    /// </summary>
    public void Run()
    {
        _listener.Start();
        _running = true;

        Console.WriteLine($"LPMud Driver listening on port {_port}");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        // Handle Ctrl+C
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Stop();
        };

        while (_running)
        {
            try
            {
                // Accept new connections (non-blocking check)
                AcceptPendingConnections();

                // Process all connections
                ProcessConnections();

                // Small sleep to avoid busy-waiting
                Thread.Sleep(10);
            }
            catch (Exception ex) when (_running)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }

        Cleanup();
    }

    /// <summary>
    /// Stop the server gracefully.
    /// </summary>
    public void Stop()
    {
        _running = false;
        Console.WriteLine("\nShutting down...");
    }

    private void AcceptPendingConnections()
    {
        while (_listener.Pending())
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                var connection = new Connection(client);

                lock (_lock)
                {
                    _connections.Add(connection);
                }

                Console.WriteLine($"New connection: {connection.Id} from {client.Client.RemoteEndPoint}");
                connection.SendWelcome();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    private void ProcessConnections()
    {
        List<Connection> toRemove = new();
        List<Connection> snapshot;

        lock (_lock)
        {
            snapshot = _connections.ToList();
        }

        foreach (var conn in snapshot)
        {
            if (!conn.IsConnected)
            {
                toRemove.Add(conn);
                continue;
            }

            try
            {
                var lines = conn.ReadAvailableLines();
                foreach (var line in lines)
                {
                    conn.ProcessLine(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {conn.Id}: {ex.Message}");
                toRemove.Add(conn);
            }
        }

        // Remove disconnected connections
        if (toRemove.Count > 0)
        {
            lock (_lock)
            {
                foreach (var conn in toRemove)
                {
                    Console.WriteLine($"Connection closed: {conn.Id}");
                    _connections.Remove(conn);
                    conn.Dispose();
                }
            }
        }
    }

    private void Cleanup()
    {
        lock (_lock)
        {
            foreach (var conn in _connections)
            {
                conn.SendLine("Server shutting down. Goodbye!");
                conn.Dispose();
            }
            _connections.Clear();
        }

        _listener.Stop();
        Console.WriteLine("Server stopped.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _listener.Stop();

        lock (_lock)
        {
            foreach (var conn in _connections)
            {
                conn.Dispose();
            }
            _connections.Clear();
        }
    }
}
