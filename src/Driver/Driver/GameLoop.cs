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

    #region Heartbeat System

    /// <summary>
    /// Objects that have heartbeats enabled.
    /// heart_beat() is called on each object every HeartbeatIntervalTicks ticks.
    /// </summary>
    private readonly HashSet<MudObject> _heartbeatObjects = new();
    private readonly object _heartbeatLock = new();

    /// <summary>
    /// Heartbeat interval in ticks (20 ticks = 2 seconds at 10 ticks/sec).
    /// </summary>
    private const int HeartbeatIntervalTicks = 20;

    /// <summary>
    /// Current tick count for heartbeat timing.
    /// </summary>
    private int _tickCount;

    #endregion

    #region Callout System

    /// <summary>
    /// Entry in the callout queue.
    /// </summary>
    private record CalloutEntry(
        MudObject Target,
        string Function,
        List<object> Args,
        DateTime FireTime,
        int CalloutId
    );

    /// <summary>
    /// Priority queue of pending callouts, ordered by fire time.
    /// </summary>
    private readonly PriorityQueue<CalloutEntry, DateTime> _callouts = new();
    private readonly object _calloutLock = new();

    /// <summary>
    /// Next callout ID to assign.
    /// </summary>
    private int _nextCalloutId = 1;

    #endregion

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
    /// The starting room path for new players.
    /// </summary>
    public string StartingRoomPath { get; set; } = "/world/rooms/town/square";

    /// <summary>
    /// Static reference to the game loop for efun callbacks.
    /// Set during initialization.
    /// </summary>
    public static GameLoop? Instance { get; private set; }

    /// <summary>
    /// Initialize the interpreter. Must be called after ObjectManager.InitializeInterpreter().
    /// </summary>
    public void InitializeInterpreter(ObjectInterpreter interpreter)
    {
        _interpreter = interpreter;
        Instance = this;

        // Wire up efun callbacks for tell_room() support
        EfunRegistry.FindSessionByPlayer = FindSessionByPlayerObject;
        EfunRegistry.SendToConnection = SendToPlayer;
    }

    /// <summary>
    /// Find a session by its player object.
    /// Used by tell_room() efun.
    /// </summary>
    private PlayerSession? FindSessionByPlayerObject(MudObject playerObject)
    {
        lock (_sessionLock)
        {
            return _sessions.Values.FirstOrDefault(s => s.PlayerObject == playerObject);
        }
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
        MudObject? startingRoom = null;

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

        // Try to load the starting room
        try
        {
            startingRoom = _objectManager.LoadObject(StartingRoomPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load starting room {StartingRoomPath}: {ex.Message}");
        }

        // Mark player as interactive and set connection info
        if (playerObject != null)
        {
            playerObject.IsInteractive = true;
            playerObject.ConnectionId = connectionId;
        }

        // Move player to starting room
        if (playerObject != null && startingRoom != null)
        {
            playerObject.MoveTo(startingRoom);
            Console.WriteLine($"Moved player to {StartingRoomPath}");
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
            // Clear interactive status before destructing
            session.PlayerObject.IsInteractive = false;
            session.PlayerObject.ConnectionId = null;

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

                // Process heartbeats every HeartbeatIntervalTicks ticks
                _tickCount++;
                if (_tickCount >= HeartbeatIntervalTicks)
                {
                    _tickCount = 0;
                    ProcessHeartbeats();
                }

                // Process pending callouts every tick
                ProcessCallouts();

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

    #region Heartbeat Methods

    /// <summary>
    /// Register an object for heartbeats.
    /// Called by set_heart_beat(1) efun.
    /// </summary>
    public void RegisterHeartbeat(MudObject obj)
    {
        lock (_heartbeatLock)
        {
            _heartbeatObjects.Add(obj);
        }
    }

    /// <summary>
    /// Unregister an object from heartbeats.
    /// Called by set_heart_beat(0) efun.
    /// </summary>
    public void UnregisterHeartbeat(MudObject obj)
    {
        lock (_heartbeatLock)
        {
            _heartbeatObjects.Remove(obj);
        }
    }

    /// <summary>
    /// Check if an object has heartbeat enabled.
    /// </summary>
    public bool HasHeartbeat(MudObject obj)
    {
        lock (_heartbeatLock)
        {
            return _heartbeatObjects.Contains(obj);
        }
    }

    /// <summary>
    /// Process heartbeats for all registered objects.
    /// Called every HeartbeatIntervalTicks ticks.
    /// </summary>
    private void ProcessHeartbeats()
    {
        if (_interpreter == null) return;

        // Get a snapshot of objects to avoid lock during execution
        List<MudObject> objects;
        lock (_heartbeatLock)
        {
            objects = _heartbeatObjects.ToList();
        }

        foreach (var obj in objects)
        {
            // Skip destructed objects and remove from registry
            if (obj.IsDestructed)
            {
                lock (_heartbeatLock)
                {
                    _heartbeatObjects.Remove(obj);
                }
                continue;
            }

            // Skip objects without heartbeat function
            if (obj.FindFunction("heart_beat") == null)
            {
                continue;
            }

            try
            {
                // Reset instruction counter for fair execution
                _interpreter.ResetInstructionCount();

                // Call heart_beat() on the object
                _interpreter.CallFunctionOnObject(obj, "heart_beat", new List<object>());
            }
            catch (ExecutionLimitException ex)
            {
                Console.WriteLine($"Heartbeat limit exceeded on {obj.ObjectName}: {ex.Message}");
                // Disable heartbeat for misbehaving object
                lock (_heartbeatLock)
                {
                    _heartbeatObjects.Remove(obj);
                }
                obj.HeartbeatEnabled = false;
            }
            catch (ReturnException)
            {
                // Normal return from heart_beat()
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat error on {obj.ObjectName}: {ex.Message}");
            }
        }
    }

    #endregion

    #region Callout Methods

    /// <summary>
    /// Schedule a callout to call a function on an object after a delay.
    /// Returns the callout ID which can be used to cancel it.
    /// </summary>
    public int ScheduleCallout(MudObject target, string function, List<object> args, int delaySeconds)
    {
        lock (_calloutLock)
        {
            var id = _nextCalloutId++;
            var entry = new CalloutEntry(
                target,
                function,
                args,
                DateTime.UtcNow.AddSeconds(delaySeconds),
                id
            );
            _callouts.Enqueue(entry, entry.FireTime);
            return id;
        }
    }

    /// <summary>
    /// Remove a callout by function name for the given object.
    /// Returns the time remaining until it would have fired, or -1 if not found.
    /// </summary>
    public int RemoveCalloutByFunction(MudObject target, string function)
    {
        lock (_calloutLock)
        {
            // We need to rebuild the queue without the matching callout
            var remaining = new List<CalloutEntry>();
            int foundTime = -1;

            while (_callouts.TryDequeue(out var entry, out _))
            {
                if (entry.Target == target && entry.Function == function && foundTime == -1)
                {
                    // Found the first matching callout
                    foundTime = Math.Max(0, (int)(entry.FireTime - DateTime.UtcNow).TotalSeconds);
                }
                else
                {
                    remaining.Add(entry);
                }
            }

            // Re-add remaining callouts
            foreach (var entry in remaining)
            {
                _callouts.Enqueue(entry, entry.FireTime);
            }

            return foundTime;
        }
    }

    /// <summary>
    /// Remove a callout by its ID.
    /// Returns the time remaining until it would have fired, or -1 if not found.
    /// </summary>
    public int RemoveCalloutById(int calloutId)
    {
        lock (_calloutLock)
        {
            var remaining = new List<CalloutEntry>();
            int foundTime = -1;

            while (_callouts.TryDequeue(out var entry, out _))
            {
                if (entry.CalloutId == calloutId && foundTime == -1)
                {
                    foundTime = Math.Max(0, (int)(entry.FireTime - DateTime.UtcNow).TotalSeconds);
                }
                else
                {
                    remaining.Add(entry);
                }
            }

            foreach (var entry in remaining)
            {
                _callouts.Enqueue(entry, entry.FireTime);
            }

            return foundTime;
        }
    }

    /// <summary>
    /// Find the time remaining until a callout fires for a given function.
    /// Returns -1 if no matching callout is found.
    /// </summary>
    public int FindCallout(MudObject target, string function)
    {
        lock (_calloutLock)
        {
            // We need to peek through the queue without modifying it
            var entries = new List<CalloutEntry>();

            while (_callouts.TryDequeue(out var entry, out _))
            {
                entries.Add(entry);
            }

            int foundTime = -1;
            foreach (var entry in entries)
            {
                _callouts.Enqueue(entry, entry.FireTime);
                if (entry.Target == target && entry.Function == function && foundTime == -1)
                {
                    foundTime = Math.Max(0, (int)(entry.FireTime - DateTime.UtcNow).TotalSeconds);
                }
            }

            return foundTime;
        }
    }

    /// <summary>
    /// Process all callouts that are due to fire.
    /// Called every tick.
    /// </summary>
    private void ProcessCallouts()
    {
        if (_interpreter == null) return;

        var now = DateTime.UtcNow;
        var toExecute = new List<CalloutEntry>();

        // Collect all due callouts
        lock (_calloutLock)
        {
            while (_callouts.TryPeek(out var entry, out var fireTime) && fireTime <= now)
            {
                _callouts.Dequeue();
                toExecute.Add(entry);
            }
        }

        // Execute outside the lock
        foreach (var entry in toExecute)
        {
            // Skip destructed objects
            if (entry.Target.IsDestructed)
            {
                continue;
            }

            // Skip if function doesn't exist
            if (entry.Target.FindFunction(entry.Function) == null)
            {
                Console.WriteLine($"Callout warning: Function {entry.Function} not found on {entry.Target.ObjectName}");
                continue;
            }

            try
            {
                // Reset instruction counter
                _interpreter.ResetInstructionCount();

                // Call the function
                _interpreter.CallFunctionOnObject(entry.Target, entry.Function, entry.Args);
            }
            catch (ExecutionLimitException ex)
            {
                Console.WriteLine($"Callout limit exceeded on {entry.Target.ObjectName}->{entry.Function}: {ex.Message}");
            }
            catch (ReturnException)
            {
                // Normal return from callout function
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Callout error on {entry.Target.ObjectName}->{entry.Function}: {ex.Message}");
            }
        }
    }

    #endregion

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

        // Check for pending input handler (set by input_to)
        var inputHandler = session.PendingInputHandler;
        if (inputHandler != null)
        {
            // Clear the handler before calling it (so it doesn't persist)
            session.PendingInputHandler = null;

            // Set up execution context
            var inputContext = new ExecutionContext
            {
                PlayerObject = session.PlayerObject,
                ConnectionId = cmd.ConnectionId,
                OutputQueue = _outputQueue
            };

            inputContext.Execute(() =>
            {
                HandleInputTo(inputHandler, cmd.Input);
            });

            // Check if player was destructed
            if (session.PlayerObject != null && session.PlayerObject.IsDestructed)
            {
                OnPlayerDisconnect?.Invoke(cmd.ConnectionId);
                return;
            }

            SendPrompt(cmd.ConnectionId);
            return;
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
    /// Handle input captured by input_to().
    /// </summary>
    private void HandleInputTo(InputHandler handler, string input)
    {
        if (_interpreter == null) return;

        // Skip if target object was destructed
        if (handler.Target.IsDestructed)
        {
            return;
        }

        // Check if the function exists
        if (handler.Target.FindFunction(handler.Function) == null)
        {
            Console.WriteLine($"input_to warning: Function {handler.Function} not found on {handler.Target.ObjectName}");
            return;
        }

        try
        {
            // Reset instruction counter
            _interpreter.ResetInstructionCount();

            // Call the function with the input
            _interpreter.CallFunctionOnObject(handler.Target, handler.Function, new List<object> { input });
        }
        catch (ExecutionLimitException ex)
        {
            Console.WriteLine($"input_to limit exceeded on {handler.Target.ObjectName}->{handler.Function}: {ex.Message}");
        }
        catch (ReturnException)
        {
            // Normal return from input handler
        }
        catch (Exception ex)
        {
            Console.WriteLine($"input_to error on {handler.Target.ObjectName}->{handler.Function}: {ex.Message}");
        }
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
            cmdObj = _objectManager.LoadObject($"/cmds/std/{verb}");
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
