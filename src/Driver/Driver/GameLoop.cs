using System.Collections.Concurrent;

namespace Driver;

/// <summary>
/// Central game loop that processes all player commands on a single thread.
/// This ensures LPC code execution is single-threaded, avoiding all race conditions.
/// Network I/O remains async on separate threads.
/// </summary>
public class GameLoop
{
    /// <summary>
    /// Commands queued from player connections, waiting to be processed.
    /// Thread-safe for concurrent enqueuing from network threads.
    /// </summary>
    private readonly ConcurrentQueue<PlayerCommand> _commandQueue = new();

    /// <summary>
    /// Output messages to be sent to connections.
    /// Thread-safe for concurrent dequeuing by network threads.
    /// </summary>
    private readonly ConcurrentQueue<OutputMessage> _outputQueue = new();

    /// <summary>
    /// Active player sessions mapped by connection ID.
    /// Protected by lock for add/remove operations.
    /// </summary>
    private readonly Dictionary<string, PlayerSession> _sessions = new();
    private readonly object _sessionLock = new();

    /// <summary>
    /// The object manager for loading and managing MUD objects.
    /// </summary>
    private readonly ObjectManager _objectManager;

    /// <summary>
    /// The object interpreter for executing LPC code.
    /// </summary>
    private ObjectInterpreter? _interpreter;

    /// <summary>
    /// The game loop thread.
    /// </summary>
    private Thread? _gameThread;

    /// <summary>
    /// Whether the game loop is running.
    /// </summary>
    private volatile bool _running;

    /// <summary>
    /// Tick interval in milliseconds (10 ticks/second).
    /// </summary>
    private const int TickIntervalMs = 100;

    /// <summary>
    /// Callback invoked when a player session should be disconnected.
    /// Set by TelnetServer to handle disconnection.
    /// </summary>
    public Action<string>? OnPlayerDisconnect { get; set; }

    public GameLoop(ObjectManager objectManager)
    {
        _objectManager = objectManager;
    }

    /// <summary>
    /// Initialize the interpreter. Must be called after ObjectManager.InitializeInterpreter().
    /// </summary>
    public void InitializeInterpreter(ObjectInterpreter interpreter)
    {
        _interpreter = interpreter;
    }

    /// <summary>
    /// Start the game loop thread.
    /// </summary>
    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _gameThread = new Thread(RunLoop)
        {
            Name = "GameLoop",
            IsBackground = true
        };
        _gameThread.Start();

        Console.WriteLine("Game loop started.");
    }

    /// <summary>
    /// Stop the game loop thread.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _gameThread?.Join(TimeSpan.FromSeconds(5));
        Console.WriteLine("Game loop stopped.");
    }

    /// <summary>
    /// Queue a command from a player connection.
    /// Called from network thread.
    /// </summary>
    public void QueueCommand(string connectionId, string input)
    {
        _commandQueue.Enqueue(new PlayerCommand
        {
            ConnectionId = connectionId,
            Input = input,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Try to dequeue an output message.
    /// Called from network thread.
    /// </summary>
    public bool TryDequeueOutput(out OutputMessage? output)
    {
        return _outputQueue.TryDequeue(out output);
    }

    /// <summary>
    /// Create a player session for a new connection.
    /// Called from network thread.
    /// </summary>
    public void CreatePlayerSession(string connectionId)
    {
        MudObject? playerObject = null;

        try
        {
            // Clone a player object for this connection
            playerObject = _objectManager.CloneObject("/std/player");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create player object: {ex.Message}");
            // Create session without player object (will use fallback behavior)
        }

        var session = new PlayerSession
        {
            ConnectionId = connectionId,
            PlayerObject = playerObject,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        lock (_sessionLock)
        {
            _sessions[connectionId] = session;
        }

        Console.WriteLine($"Created player session for {connectionId}" +
            (playerObject != null ? $" with player object {playerObject.ObjectName}" : ""));
    }

    /// <summary>
    /// Remove a player session when connection closes.
    /// Called from network thread.
    /// </summary>
    public void RemovePlayerSession(string connectionId)
    {
        PlayerSession? session = null;

        lock (_sessionLock)
        {
            if (_sessions.TryGetValue(connectionId, out session))
            {
                _sessions.Remove(connectionId);
            }
        }

        if (session?.PlayerObject != null && !session.PlayerObject.IsDestructed)
        {
            try
            {
                _objectManager.DestructObject(session.PlayerObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error destructing player object: {ex.Message}");
            }
        }

        Console.WriteLine($"Removed player session for {connectionId}");
    }

    /// <summary>
    /// Get a player session by connection ID.
    /// </summary>
    public PlayerSession? GetSession(string connectionId)
    {
        lock (_sessionLock)
        {
            return _sessions.TryGetValue(connectionId, out var session) ? session : null;
        }
    }

    /// <summary>
    /// Get all active sessions.
    /// </summary>
    public List<PlayerSession> GetAllSessions()
    {
        lock (_sessionLock)
        {
            return _sessions.Values.ToList();
        }
    }

    /// <summary>
    /// Send output to a specific player.
    /// </summary>
    public void SendToPlayer(string connectionId, string message)
    {
        _outputQueue.Enqueue(new OutputMessage
        {
            ConnectionId = connectionId,
            Content = message
        });
    }

    /// <summary>
    /// The main game loop.
    /// </summary>
    private void RunLoop()
    {
        while (_running)
        {
            try
            {
                var tickStart = DateTime.UtcNow;

                // Process all queued commands
                ProcessCommands();

                // TODO Milestone 9: Process heartbeats
                // TODO Milestone 9: Process callouts

                // Sleep for remaining tick time
                var elapsed = (DateTime.UtcNow - tickStart).TotalMilliseconds;
                var sleepTime = Math.Max(0, TickIntervalMs - elapsed);
                if (sleepTime > 0)
                {
                    Thread.Sleep((int)sleepTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Game loop error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Process all queued commands.
    /// </summary>
    private void ProcessCommands()
    {
        // Process up to 100 commands per tick to avoid starvation
        int processed = 0;
        while (processed < 100 && _commandQueue.TryDequeue(out var cmd))
        {
            ProcessCommand(cmd);
            processed++;
        }
    }

    /// <summary>
    /// Process a single command.
    /// </summary>
    private void ProcessCommand(PlayerCommand cmd)
    {
        // Get the player's session
        PlayerSession? session;
        lock (_sessionLock)
        {
            if (!_sessions.TryGetValue(cmd.ConnectionId, out session))
            {
                // Connection was closed, discard command
                return;
            }

            // Update last activity
            session.LastActivity = DateTime.UtcNow;
        }

        // Parse the command
        var input = cmd.Input.Trim();
        if (string.IsNullOrEmpty(input))
        {
            SendPrompt(cmd.ConnectionId);
            return;
        }

        // Split into verb and args
        var spaceIndex = input.IndexOf(' ');
        string verb, args;
        if (spaceIndex >= 0)
        {
            verb = input[..spaceIndex].ToLowerInvariant();
            args = input[(spaceIndex + 1)..];
        }
        else
        {
            verb = input.ToLowerInvariant();
            args = "";
        }

        // Set up execution context
        var context = new ExecutionContext
        {
            PlayerObject = session.PlayerObject,
            ConnectionId = cmd.ConnectionId,
            OutputQueue = _outputQueue
        };

        context.Execute(() =>
        {
            ExecuteCommand(session, verb, args);
        });

        // Check if player was destructed (quit command)
        if (session.PlayerObject != null && session.PlayerObject.IsDestructed)
        {
            // Mark for disconnection
            OnPlayerDisconnect?.Invoke(cmd.ConnectionId);
            return; // Don't send prompt
        }

        SendPrompt(cmd.ConnectionId);
    }

    /// <summary>
    /// Execute a command for a player.
    /// </summary>
    private void ExecuteCommand(PlayerSession session, string verb, string args)
    {
        // Try to load the command object
        MudObject? cmdObj = null;
        try
        {
            cmdObj = _objectManager.LoadObject($"/cmds/{verb}");
        }
        catch (ObjectManagerException)
        {
            // Command not found
            SendToPlayer(session.ConnectionId, $"Unknown command: {verb}\r\n");
            return;
        }

        // Find and call the main function
        var mainFunc = cmdObj.FindFunction("main");
        if (mainFunc == null)
        {
            SendToPlayer(session.ConnectionId, $"Command '{verb}' has no main() function.\r\n");
            return;
        }

        try
        {
            // Reset instruction counter before each command
            _interpreter?.ResetInstructionCount();

            // Execute the command's main function with args
            _interpreter?.CallFunctionOnObject(cmdObj, "main", new List<object> { args });
        }
        catch (ReturnException)
        {
            // Normal return from command - ignore
        }
        catch (ExecutionLimitException ex)
        {
            // Execution limit exceeded - report to player
            SendToPlayer(session.ConnectionId, $"EXECUTION ABORTED: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            SendToPlayer(session.ConnectionId, $"Error: {ex.Message}\r\n");
        }
    }

    /// <summary>
    /// Send a prompt to a player.
    /// </summary>
    private void SendPrompt(string connectionId)
    {
        SendToPlayer(connectionId, "> ");
    }
}
