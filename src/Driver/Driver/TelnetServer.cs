using System.Net;
using System.Net.Sockets;

namespace Driver;

/// <summary>
/// Telnet server that accepts multiple concurrent connections.
/// Commands are queued to the GameLoop for single-threaded processing.
/// </summary>
public class TelnetServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly List<Connection> _connections = new();
    private readonly int _port;
    private readonly object _lock = new();

    private readonly GameLoop _gameLoop;

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

    /// <summary>
    /// Connections pending disconnection (set by game loop when player is destructed).
    /// </summary>
    private readonly HashSet<string> _pendingDisconnect = new();
    private readonly object _disconnectLock = new();

    public TelnetServer(int port, GameLoop gameLoop)
    {
        _port = port;
        _gameLoop = gameLoop;
        _listener = new TcpListener(IPAddress.Any, port);

        // Set up callback for when players should be disconnected
        _gameLoop.OnPlayerDisconnect = connectionId =>
        {
            lock (_disconnectLock)
            {
                _pendingDisconnect.Add(connectionId);
            }
        };

        // Set up callback for echo mode changes (password input)
        _gameLoop.OnSetEchoMode = (connectionId, enabled) =>
        {
            var conn = FindConnection(connectionId);
            conn?.SetEchoMode(enabled);
        };
    }

    /// <summary>
    /// Start the server and run the main loop.
    /// This blocks until Stop() is called.
    /// </summary>
    public void Run()
    {
        _listener.Start();
        _running = true;

        Console.WriteLine($"LPMud Revival listening on port {_port}");
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

                // Process all connections (read input, drain output)
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
        Console.WriteLine("\nShutting down...");

        // Perform graceful shutdown (announce to players, save data)
        _gameLoop.GracefulShutdown();

        _running = false;
    }

    private void AcceptPendingConnections()
    {
        while (_listener.Pending())
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                var connection = new Connection(client, _gameLoop);

                lock (_lock)
                {
                    _connections.Add(connection);
                }

                Console.WriteLine($"New connection: {connection.Id} from {client.Client.RemoteEndPoint}");

                // Create player session in game loop (sends welcome banner)
                _gameLoop.CreatePlayerSession(connection.Id);
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

        // Drain output queue and send to connections
        DrainOutputQueue(snapshot);

        // Check for pending disconnects from game loop
        HashSet<string> disconnectSet;
        lock (_disconnectLock)
        {
            disconnectSet = new HashSet<string>(_pendingDisconnect);
            _pendingDisconnect.Clear();
        }

        // Process input from connections
        foreach (var conn in snapshot)
        {
            // Check if marked for disconnection
            if (disconnectSet.Contains(conn.Id))
            {
                toRemove.Add(conn);
                continue;
            }

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

                    // Remove player session from game loop
                    _gameLoop.RemovePlayerSession(conn.Id);

                    _connections.Remove(conn);
                    conn.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Drain output queue and send messages to appropriate connections.
    /// </summary>
    private void DrainOutputQueue(List<Connection> connections)
    {
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            if (output == null) continue;

            var conn = connections.Find(c => c.Id == output.ConnectionId);
            conn?.Send(output.Content);
        }
    }

    private Connection? FindConnection(string connectionId)
    {
        lock (_lock)
        {
            return _connections.Find(c => c.Id == connectionId);
        }
    }

    private void Cleanup()
    {
        lock (_lock)
        {
            foreach (var conn in _connections)
            {
                conn.SendLine("Server shutting down. Goodbye!");

                // Remove player session from game loop
                _gameLoop.RemovePlayerSession(conn.Id);

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
                _gameLoop.RemovePlayerSession(conn.Id);
                conn.Dispose();
            }
            _connections.Clear();
        }
    }
}
