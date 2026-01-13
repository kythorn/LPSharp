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

    /// <summary>
    /// Linkdead sessions mapped by username (case-insensitive).
    /// These sessions have lost their connection but persist for reconnection.
    /// Protected by _sessionLock.
    /// </summary>
    private readonly Dictionary<string, PlayerSession> _linkdeadSessions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// How long linkdead sessions persist before being cleaned up.
    /// </summary>
    private static readonly TimeSpan LinkdeadTimeout = TimeSpan.FromMinutes(15);

    private readonly object _sessionLock = new();
    private readonly object _loginLock = new(); // Serialize login completion to prevent race conditions

    /// <summary>
    /// The object manager for loading and managing MUD objects.
    /// </summary>
    private readonly ObjectManager _objectManager;

    /// <summary>
    /// The account manager for authentication.
    /// Exposed for permission checks in efuns.
    /// </summary>
    public AccountManager AccountManager => _accountManager;
    private readonly AccountManager _accountManager;

    /// <summary>
    /// Rate limiter for preventing command flooding.
    /// </summary>
    public RateLimiter RateLimiter => _rateLimiter;
    private readonly RateLimiter _rateLimiter = new();

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

    /// <summary>
    /// When the last periodic save occurred.
    /// </summary>
    private DateTime _lastPeriodicSave = DateTime.UtcNow;

    /// <summary>
    /// How often to auto-save all players.
    /// </summary>
    private static readonly TimeSpan PeriodicSaveInterval = TimeSpan.FromMinutes(5);

    #endregion

    #region Reset System

    /// <summary>
    /// Objects registered for periodic reset() calls.
    /// Maps object to its next reset time.
    /// </summary>
    private readonly Dictionary<MudObject, DateTime> _resetObjects = new();
    private readonly object _resetLock = new();

    /// <summary>
    /// Default reset interval in seconds.
    /// </summary>
    private const int DefaultResetIntervalSeconds = 60;

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

    #region Command Resolution System

    /// <summary>
    /// Protected commands that can NEVER be overridden by add_action or aliased.
    /// These are critical for player safety and game integrity.
    /// </summary>
    private static readonly HashSet<string> ProtectedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "quit", "save", "password", "who", "tell", "shout", "bug", "typo", "idea", "alias", "unalias"
    };

    /// <summary>
    /// Check if a command name is protected (cannot be aliased or overridden).
    /// </summary>
    public static bool IsProtectedCommand(string command)
    {
        return ProtectedCommands.Contains(command);
    }

    /// <summary>
    /// Core commands that have standard implementations in /cmds/.
    /// These can only be overridden by add_action with the OverrideCore flag.
    /// </summary>
    private static readonly HashSet<string> CoreCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "look", "go", "get", "take", "drop", "put", "give",
        "inventory", "score", "say", "emote"
    };

    /// <summary>
    /// Resolve a command alias using the player's alias dictionary.
    /// Transforms verb and args if an alias matches.
    /// Returns the (possibly transformed) verb and args.
    /// </summary>
    private static (string Verb, string Args) ResolveAlias(PlayerSession session, string verb, string args)
    {
        if (session.Aliases.TryGetValue(verb, out var aliasCommand))
        {
            // Parse the alias command into verb and args
            var spaceIdx = aliasCommand.IndexOf(' ');
            string newVerb, aliasArgs;
            if (spaceIdx >= 0)
            {
                newVerb = aliasCommand[..spaceIdx];
                aliasArgs = aliasCommand[(spaceIdx + 1)..];
            }
            else
            {
                newVerb = aliasCommand;
                aliasArgs = "";
            }

            // Combine alias args with original args
            var newArgs = string.IsNullOrEmpty(aliasArgs)
                ? args
                : string.IsNullOrEmpty(args) ? aliasArgs : $"{aliasArgs} {args}";

            return (newVerb, newArgs);
        }
        return (verb, args);
    }

    #endregion

    /// <summary>
    /// Callback invoked when a player session should be disconnected.
    /// Set by TelnetServer to handle disconnection.
    /// </summary>
    public Action<string>? OnPlayerDisconnect { get; set; }

    /// <summary>
    /// Callback invoked to set echo mode on a connection.
    /// Set by TelnetServer to handle password input.
    /// </summary>
    public Action<string, bool>? OnSetEchoMode { get; set; }

    public GameLoop(ObjectManager objectManager, AccountManager accountManager)
    {
        _objectManager = objectManager;
        _accountManager = accountManager;
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

        Logger.Info("Game loop started", LogCategory.System);
    }

    /// <summary>
    /// Stop the game loop thread.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _gameThread?.Join(TimeSpan.FromSeconds(5));
        Logger.Info("Game loop stopped", LogCategory.System);
    }

    /// <summary>
    /// Perform graceful shutdown: announce to players, save all data.
    /// Call this before Stop() for a clean shutdown.
    /// </summary>
    public void GracefulShutdown()
    {
        Logger.Info("Initiating graceful shutdown...", LogCategory.System);

        // Get all active sessions
        List<PlayerSession> sessions;
        lock (_sessionLock)
        {
            sessions = _sessions.Values
                .Where(s => s.LoginState == LoginState.Playing)
                .ToList();
        }

        // Announce shutdown to all players
        foreach (var session in sessions)
        {
            if (!string.IsNullOrEmpty(session.ConnectionId))
            {
                SendToPlayer(session.ConnectionId, "\r\n*** Server shutting down. Saving your character... ***\r\n");
            }
        }

        // Save all players (includes linkdead)
        SaveAllPlayers();

        Logger.Info("Graceful shutdown complete", LogCategory.System);
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
    /// Does NOT create a player object - that happens after authentication.
    /// Called from network thread.
    /// </summary>
    public void CreatePlayerSession(string connectionId)
    {
        var session = new PlayerSession
        {
            ConnectionId = connectionId,
            PlayerObject = null,  // No player until authenticated
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            LoginState = LoginState.Welcome
        };

        lock (_sessionLock)
        {
            _sessions[connectionId] = session;
        }

        Logger.Debug($"Created login session for {connectionId}", LogCategory.Network);

        // Send welcome banner
        SendWelcomeBanner(connectionId);
    }

    /// <summary>
    /// Send the welcome banner and initial login prompt.
    /// </summary>
    private void SendWelcomeBanner(string connectionId)
    {
        SendToPlayer(connectionId, "\r\n");
        SendToPlayer(connectionId, "========================================\r\n");
        SendToPlayer(connectionId, "       Welcome to LPMud Revival!        \r\n");
        SendToPlayer(connectionId, "========================================\r\n");
        SendToPlayer(connectionId, "\r\n");
        SendToPlayer(connectionId, "Enter your name, or type 'new' to create a character: ");

        var session = GetSession(connectionId);
        if (session != null)
        {
            session.LoginState = LoginState.AwaitingName;
        }
    }

    /// <summary>
    /// Remove a player session when connection closes.
    /// If the player was actively playing, they go linkdead instead of being destroyed.
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

                // If player was actively playing, move to linkdead instead of destroying
                if (session.LoginState == LoginState.Playing &&
                    session.AuthenticatedUsername != null &&
                    session.PlayerObject != null &&
                    !session.PlayerObject.IsDestructed)
                {
                    session.IsLinkdead = true;
                    session.LinkdeadSince = DateTime.UtcNow;
                    session.ConnectionId = string.Empty; // No longer connected
                    session.PlayerObject.IsInteractive = false;
                    session.PlayerObject.ConnectionId = null;

                    _linkdeadSessions[session.AuthenticatedUsername] = session;

                    Logger.Info($"Player {session.AuthenticatedUsername} went linkdead (15 min timeout)", LogCategory.Player);

                    // Announce to the room (use character name, not account name)
                    AnnounceToRoom(session.PlayerObject, $"{GetPlayerName(session.PlayerObject, session.AuthenticatedUsername)} has gone linkdead.\r\n");
                    return;
                }
            }
        }

        // Non-playing session or no player object - just clean up
        if (session?.PlayerObject != null && !session.PlayerObject.IsDestructed)
        {
            session.PlayerObject.IsInteractive = false;
            session.PlayerObject.ConnectionId = null;

            try
            {
                _objectManager.DestructObject(session.PlayerObject);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error destructing player object: {ex.Message}", LogCategory.Object);
            }
        }

        // Clean up rate limiter data for this connection
        _rateLimiter.RemoveConnection(connectionId);

        Logger.Debug($"Removed player session for {connectionId}", LogCategory.Network);
    }

    /// <summary>
    /// Force-remove a session completely (for kicks and cleanup).
    /// Does not go linkdead - fully destroys the session.
    /// </summary>
    private void ForceRemoveSession(PlayerSession session)
    {
        lock (_sessionLock)
        {
            // Remove from active sessions if present
            if (!string.IsNullOrEmpty(session.ConnectionId))
            {
                _sessions.Remove(session.ConnectionId);
            }

            // Remove from linkdead if present
            if (session.AuthenticatedUsername != null)
            {
                _linkdeadSessions.Remove(session.AuthenticatedUsername);
            }
        }

        // Destruct the player object
        if (session.PlayerObject != null && !session.PlayerObject.IsDestructed)
        {
            session.PlayerObject.IsInteractive = false;
            session.PlayerObject.ConnectionId = null;

            try
            {
                _objectManager.DestructObject(session.PlayerObject);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error destructing player object: {ex.Message}", LogCategory.Object);
            }
        }
    }

    /// <summary>
    /// Get the display name for a player object.
    /// Uses query_name() if available, otherwise falls back to capitalized username.
    /// </summary>
    private string GetPlayerName(MudObject? player, string? fallback = null)
    {
        if (player == null || player.IsDestructed)
        {
            return fallback != null ? Capitalize(fallback) : "Someone";
        }

        // Try to call query_name() on the object
        if (_interpreter != null && player.FindFunction("query_name") != null)
        {
            try
            {
                var result = _interpreter.CallFunctionOnObject(player, "query_name", new List<object>());
                if (result is string name && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch
            {
                // Fall through to fallback
            }
        }

        return fallback != null ? Capitalize(fallback) : "Someone";
    }

    /// <summary>
    /// Announce a message to all other players in the same room.
    /// </summary>
    private void AnnounceToRoom(MudObject player, string message)
    {
        var room = player.Environment;
        if (room == null) return;

        foreach (var obj in room.Contents.ToList())
        {
            if (obj != player && obj.IsInteractive && !string.IsNullOrEmpty(obj.ConnectionId))
            {
                SendToPlayer(obj.ConnectionId, message);
            }
        }
    }

    /// <summary>
    /// Save a player object's data by calling save_player() on it.
    /// Returns true if save was successful.
    /// </summary>
    private bool SavePlayerObject(MudObject? playerObject)
    {
        if (_interpreter == null || playerObject == null || playerObject.IsDestructed)
        {
            return false;
        }

        // Check if the player has a save_player function
        if (playerObject.FindFunction("save_player") == null)
        {
            return false;
        }

        try
        {
            _interpreter.ResetInstructionCount();
            var result = _interpreter.CallFunctionOnObject(playerObject, "save_player", new List<object>());

            // save_player returns 1 on success
            if (result is int i && i == 1)
            {
                return true;
            }
            if (result is long l && l == 1)
            {
                return true;
            }
        }
        catch (ReturnException ex)
        {
            if (ex.Value is int i && i == 1)
            {
                return true;
            }
            if (ex.Value is long l && l == 1)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving player: {ex.Message}", LogCategory.Player);
        }

        return false;
    }

    /// <summary>
    /// Save all active players. Used for periodic saves and shutdown.
    /// </summary>
    public void SaveAllPlayers()
    {
        List<PlayerSession> sessions;
        lock (_sessionLock)
        {
            sessions = _sessions.Values
                .Where(s => s.LoginState == LoginState.Playing && s.PlayerObject != null)
                .ToList();

            // Also include linkdead sessions
            sessions.AddRange(_linkdeadSessions.Values
                .Where(s => s.PlayerObject != null && !s.PlayerObject.IsDestructed));
        }

        int saved = 0;
        foreach (var session in sessions)
        {
            if (SavePlayerObject(session.PlayerObject))
            {
                saved++;
            }
        }

        if (saved > 0)
        {
            Logger.Debug($"Auto-saved {saved} player(s)", LogCategory.Player);
        }
    }

    /// <summary>
    /// Clean up linkdead sessions that have exceeded the timeout.
    /// Called periodically from the game loop.
    /// </summary>
    private void CleanupExpiredLinkdeadSessions()
    {
        var now = DateTime.UtcNow;
        List<PlayerSession> expiredSessions;

        lock (_sessionLock)
        {
            expiredSessions = _linkdeadSessions.Values
                .Where(s => s.LinkdeadSince.HasValue &&
                           (now - s.LinkdeadSince.Value) > LinkdeadTimeout)
                .ToList();
        }

        foreach (var session in expiredSessions)
        {
            Logger.Info($"Linkdead session for {session.AuthenticatedUsername} expired - cleaning up", LogCategory.Player);

            // Save player data before cleanup
            if (session.PlayerObject != null && !session.PlayerObject.IsDestructed)
            {
                if (SavePlayerObject(session.PlayerObject))
                {
                    Logger.Debug($"Saved player data for {session.AuthenticatedUsername}", LogCategory.Player);
                }

                // Announce to room before cleanup (use character name, not account name)
                AnnounceToRoom(session.PlayerObject,
                    $"{GetPlayerName(session.PlayerObject, session.AuthenticatedUsername)} has disconnected (linkdead timeout).\r\n");
            }

            // Force remove (destroys player object)
            ForceRemoveSession(session);
        }
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
    /// Get all active sessions (not including linkdead).
    /// </summary>
    public List<PlayerSession> GetAllSessions()
    {
        lock (_sessionLock)
        {
            return _sessions.Values.ToList();
        }
    }

    /// <summary>
    /// Get all linkdead sessions.
    /// </summary>
    public List<PlayerSession> GetLinkdeadSessions()
    {
        lock (_sessionLock)
        {
            return _linkdeadSessions.Values.ToList();
        }
    }

    /// <summary>
    /// Find an active (Playing) session by authenticated username.
    /// Only returns sessions that are fully logged in and playing.
    /// Returns null if no playing session exists for that username.
    /// </summary>
    private PlayerSession? FindSessionByUsername(string username)
    {
        lock (_sessionLock)
        {
            return _sessions.Values.FirstOrDefault(s =>
                s.LoginState == LoginState.Playing &&
                s.AuthenticatedUsername != null &&
                s.AuthenticatedUsername.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Find a linkdead session by username.
    /// Returns null if no linkdead session exists for that username.
    /// </summary>
    private PlayerSession? FindLinkdeadSession(string username)
    {
        lock (_sessionLock)
        {
            return _linkdeadSessions.TryGetValue(username, out var session) ? session : null;
        }
    }

    /// <summary>
    /// Kick an existing session (for duplicate login handling).
    /// Notifies the player, cleans up the session, and disconnects.
    /// Does NOT go linkdead - fully destroys the session.
    /// </summary>
    private void KickSession(PlayerSession session, string reason)
    {
        // Notify the player being kicked (if connected)
        if (!string.IsNullOrEmpty(session.ConnectionId))
        {
            SendToPlayer(session.ConnectionId, $"\r\n*** {reason} ***\r\n");
        }

        // Store connection ID before cleanup
        var connectionId = session.ConnectionId;

        // Force remove the session (bypasses linkdead, fully destroys)
        ForceRemoveSession(session);

        // Disconnect the old connection (if it was connected)
        if (!string.IsNullOrEmpty(connectionId))
        {
            OnPlayerDisconnect?.Invoke(connectionId);
        }

        Logger.Info($"Kicked session: {reason}", LogCategory.Network);
    }

    /// <summary>
    /// Check if there's a linkdead or active session for the same username.
    /// Linkdead sessions are automatically reconnected.
    /// Active sessions prompt for confirmation.
    /// Returns true if handled (caller should stop), false to proceed with new login.
    /// </summary>
    private bool CheckAndPromptForExistingSession(PlayerSession session)
    {
        // Check for linkdead session first - auto-reconnect (no prompt)
        var linkdeadSession = FindLinkdeadSession(session.AuthenticatedUsername!);
        if (linkdeadSession != null)
        {
            ReconnectToLinkdeadSession(session, linkdeadSession);
            return true; // Handled - don't create new session
        }

        // Check for active session (takeover prompt)
        var existingSession = FindSessionByUsername(session.AuthenticatedUsername!);
        if (existingSession != null && existingSession.ConnectionId != session.ConnectionId)
        {
            session.LoginState = LoginState.ConfirmTakeover;
            SendToPlayer(session.ConnectionId,
                "You are already logged in from another location.\r\n" +
                "Do you want to take over that session? (y/n): ");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handle response to duplicate session takeover prompt.
    /// </summary>
    private void HandleConfirmTakeover(PlayerSession session, string input)
    {
        input = input.Trim().ToLower();

        if (input == "y" || input == "yes")
        {
            // User confirmed - kick the old session if it still exists
            var existingSession = FindSessionByUsername(session.AuthenticatedUsername!);
            if (existingSession != null && existingSession.ConnectionId != session.ConnectionId)
            {
                KickSession(existingSession, "Another login detected - you have been disconnected");
            }

            // Now complete the login
            CompleteLogin(session);
        }
        else if (input == "n" || input == "no")
        {
            SendToPlayer(session.ConnectionId, "Login cancelled.\r\n");
            OnPlayerDisconnect?.Invoke(session.ConnectionId);
        }
        else
        {
            SendToPlayer(session.ConnectionId, "Please enter 'y' or 'n': ");
        }
    }

    /// <summary>
    /// Reconnect a new connection to a linkdead session.
    /// Takes over the existing player object and session state.
    /// </summary>
    private void ReconnectToLinkdeadSession(PlayerSession newSession, PlayerSession linkdeadSession)
    {
        lock (_sessionLock)
        {
            // Remove from linkdead dictionary
            _linkdeadSessions.Remove(linkdeadSession.AuthenticatedUsername!);

            // Remove the new (empty) session from active sessions
            _sessions.Remove(newSession.ConnectionId);

            // Update linkdead session with new connection
            linkdeadSession.ConnectionId = newSession.ConnectionId;
            linkdeadSession.IsLinkdead = false;
            linkdeadSession.LinkdeadSince = null;
            linkdeadSession.LastActivity = DateTime.UtcNow;

            // Restore interactive status on player object
            if (linkdeadSession.PlayerObject != null)
            {
                linkdeadSession.PlayerObject.IsInteractive = true;
                linkdeadSession.PlayerObject.ConnectionId = newSession.ConnectionId;
            }

            // Add to active sessions with new connection ID
            _sessions[newSession.ConnectionId] = linkdeadSession;
        }

        // Announce reconnection to the room (use character name, not account name)
        if (linkdeadSession.PlayerObject != null)
        {
            AnnounceToRoom(linkdeadSession.PlayerObject,
                $"{GetPlayerName(linkdeadSession.PlayerObject, linkdeadSession.AuthenticatedUsername)} has reconnected.\r\n");
        }

        SendToPlayer(linkdeadSession.ConnectionId, "Reconnected!\r\n\r\n");

        // Show the room
        ExecuteLookForPlayer(linkdeadSession);
        SendPrompt(linkdeadSession.ConnectionId);

        Logger.Info($"Player {linkdeadSession.AuthenticatedUsername} reconnected from linkdead", LogCategory.Player);
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

                    // Clean up expired linkdead sessions (check during heartbeat cycle)
                    CleanupExpiredLinkdeadSessions();
                }

                // Periodic player saves
                var now = DateTime.UtcNow;
                if (now - _lastPeriodicSave >= PeriodicSaveInterval)
                {
                    _lastPeriodicSave = now;
                    SaveAllPlayers();

                    // Clean up rate limiter data periodically (every 5 min with saves)
                    _rateLimiter.Cleanup();
                }

                // Process pending callouts every tick
                ProcessCallouts();

                // Process object resets
                ProcessResets();

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
                Logger.Error($"Game loop error: {ex.Message}", LogCategory.System);
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
                Logger.Warning($"Heartbeat limit exceeded on {obj.ObjectName}: {ex.Message}", LogCategory.LPC);
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
                Logger.Warning($"Heartbeat error on {obj.ObjectName}: {ex.Message}", LogCategory.LPC);
            }
        }
    }

    #endregion

    #region Reset Methods

    /// <summary>
    /// Register an object for periodic reset() calls.
    /// The reset will fire after intervalSeconds, and repeat at that interval.
    /// Called by set_reset(seconds) efun.
    /// </summary>
    public void RegisterReset(MudObject obj, int intervalSeconds)
    {
        if (intervalSeconds <= 0)
        {
            UnregisterReset(obj);
            return;
        }

        lock (_resetLock)
        {
            obj.ResetInterval = intervalSeconds;
            _resetObjects[obj] = DateTime.UtcNow.AddSeconds(intervalSeconds);
        }
    }

    /// <summary>
    /// Unregister an object from periodic resets.
    /// Called by set_reset(0) efun.
    /// </summary>
    public void UnregisterReset(MudObject obj)
    {
        lock (_resetLock)
        {
            obj.ResetInterval = 0;
            _resetObjects.Remove(obj);
        }
    }

    /// <summary>
    /// Get the reset interval for an object.
    /// Returns 0 if not registered for reset.
    /// </summary>
    public int GetResetInterval(MudObject obj)
    {
        lock (_resetLock)
        {
            return _resetObjects.ContainsKey(obj) ? obj.ResetInterval : 0;
        }
    }

    /// <summary>
    /// Call reset() immediately on an object.
    /// Used after create() completes.
    /// </summary>
    public void CallReset(MudObject obj)
    {
        if (_interpreter == null) return;

        // Skip if no reset function exists
        if (obj.FindFunction("reset") == null)
        {
            return;
        }

        try
        {
            _interpreter.ResetInstructionCount();
            _interpreter.CallFunctionOnObject(obj, "reset", new List<object>());
        }
        catch (ReturnException)
        {
            // Normal return
        }
        catch (Exception ex)
        {
            Logger.Warning($"Reset error on {obj.ObjectName}: {ex.Message}", LogCategory.LPC);
        }
    }

    /// <summary>
    /// Process periodic resets for all registered objects.
    /// Called every tick, but only fires resets that are due.
    /// </summary>
    private void ProcessResets()
    {
        if (_interpreter == null) return;

        var now = DateTime.UtcNow;
        List<MudObject> dueForReset;

        // Get list of objects due for reset
        lock (_resetLock)
        {
            dueForReset = _resetObjects
                .Where(kvp => kvp.Value <= now)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        foreach (var obj in dueForReset)
        {
            // Skip destructed objects and remove from registry
            if (obj.IsDestructed)
            {
                lock (_resetLock)
                {
                    _resetObjects.Remove(obj);
                }
                continue;
            }

            // Skip if no reset function
            if (obj.FindFunction("reset") == null)
            {
                continue;
            }

            try
            {
                _interpreter.ResetInstructionCount();
                _interpreter.CallFunctionOnObject(obj, "reset", new List<object>());

                // Schedule next reset
                lock (_resetLock)
                {
                    if (_resetObjects.ContainsKey(obj) && obj.ResetInterval > 0)
                    {
                        _resetObjects[obj] = DateTime.UtcNow.AddSeconds(obj.ResetInterval);
                    }
                }
            }
            catch (ExecutionLimitException ex)
            {
                Logger.Warning($"Reset limit exceeded on {obj.ObjectName}: {ex.Message}", LogCategory.LPC);
                // Disable reset for misbehaving object
                lock (_resetLock)
                {
                    _resetObjects.Remove(obj);
                }
                obj.ResetInterval = 0;
            }
            catch (ReturnException)
            {
                // Normal return - schedule next reset
                lock (_resetLock)
                {
                    if (_resetObjects.ContainsKey(obj) && obj.ResetInterval > 0)
                    {
                        _resetObjects[obj] = DateTime.UtcNow.AddSeconds(obj.ResetInterval);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Reset error on {obj.ObjectName}: {ex.Message}", LogCategory.LPC);
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
                Logger.Warning($"Callout warning: Function {entry.Function} not found on {entry.Target.ObjectName}", LogCategory.LPC);
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
                Logger.Warning($"Callout limit exceeded on {entry.Target.ObjectName}->{entry.Function}: {ex.Message}", LogCategory.LPC);
            }
            catch (ReturnException)
            {
                // Normal return from callout function
            }
            catch (Exception ex)
            {
                Logger.Error($"Callout error on {entry.Target.ObjectName}->{entry.Function}: {ex.Message}", LogCategory.LPC);
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

        // Check rate limiting
        if (!_rateLimiter.AllowCommand(cmd.ConnectionId))
        {
            SendToPlayer(cmd.ConnectionId, "You are sending commands too quickly. Please slow down.\r\n");
            Logger.Warning($"Rate limited: {cmd.ConnectionId}", LogCategory.Network);
            return;
        }

        // Handle login states before player object exists
        if (session.LoginState != LoginState.Playing)
        {
            ProcessLoginInput(session, cmd.Input);
            return;
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

        // Resolve command aliases (e.g., "n" -> "go north")
        (verb, args) = ResolveAlias(session, verb, args);

        // Set up execution context
        var context = new ExecutionContext
        {
            PlayerObject = session.PlayerObject,
            ConnectionId = cmd.ConnectionId,
            OutputQueue = _outputQueue,
            CurrentVerb = verb,
            CurrentArgs = args,
            NotifyFailMessage = null
        };

        context.Execute(() =>
        {
            ExecuteCommand(context, session, verb, args);
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
            Logger.Warning($"input_to warning: Function {handler.Function} not found on {handler.Target.ObjectName}", LogCategory.LPC);
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
            Logger.Warning($"input_to limit exceeded on {handler.Target.ObjectName}->{handler.Function}: {ex.Message}", LogCategory.LPC);
        }
        catch (ReturnException)
        {
            // Normal return from input handler
        }
        catch (Exception ex)
        {
            Logger.Error($"input_to error on {handler.Target.ObjectName}->{handler.Function}: {ex.Message}", LogCategory.LPC);
        }
    }

    /// <summary>
    /// Execute a command for a player using three-tier resolution:
    /// 1. Protected commands (always /cmds/, never overridable)
    /// 2. Core commands (/cmds/ unless OverrideCore flag set)
    /// 3. add_action handlers from room, room contents, inventory
    /// 4. Fallback to /cmds/ for any other verb
    /// </summary>
    private void ExecuteCommand(ExecutionContext context, PlayerSession session, string verb, string args)
    {
        // Tier 1: Protected commands - always use /cmds/, never allow override
        if (ProtectedCommands.Contains(verb))
        {
            if (!ExecuteCmdFile(session, verb, args))
            {
                SendToPlayer(session.ConnectionId, $"Unknown command: {verb}\r\n");
            }
            return;
        }

        // Tier 2: Check for add_action handlers with OverrideCore flag for core commands
        if (CoreCommands.Contains(verb))
        {
            // Check if any action has OverrideCore flag
            var overrideAction = FindActionWithOverride(session.PlayerObject, verb);
            if (overrideAction != null)
            {
                if (TryExecuteAction(context, session, overrideAction, args))
                {
                    return;
                }
            }

            // Otherwise, use the /cmds/ implementation
            if (ExecuteCmdFile(session, verb, args))
            {
                return;
            }
            // If /cmds/ file doesn't exist, fall through to add_action handlers
        }

        // Tier 3: Check add_action handlers (for non-core commands, or core without /cmds/ file)
        var actions = CollectActions(session.PlayerObject, verb);
        foreach (var action in actions)
        {
            if (TryExecuteAction(context, session, action, args))
            {
                return; // Action handled the command
            }
        }

        // Tier 4: Fallback to /cmds/ for any verb (non-core commands)
        if (!CoreCommands.Contains(verb))
        {
            if (ExecuteCmdFile(session, verb, args))
            {
                return;
            }
        }

        // No handler found - show notify_fail message or default
        var failMessage = context.NotifyFailMessage ?? "What?\r\n";
        SendToPlayer(session.ConnectionId, failMessage);
    }

    /// <summary>
    /// Execute a command immediately (for command() efun).
    /// Returns true if the command was handled.
    /// </summary>
    public bool ExecuteCommandImmediate(ExecutionContext context, string cmdString)
    {
        if (_interpreter == null || context.PlayerObject == null)
        {
            return false;
        }

        // Parse verb and args
        var input = cmdString.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        // Get session for player (needed for alias resolution)
        PlayerSession? session;
        lock (_sessionLock)
        {
            session = _sessions.Values.FirstOrDefault(s => s.PlayerObject == context.PlayerObject);
        }

        if (session == null)
        {
            return false;
        }

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

        // Resolve command aliases (e.g., "n" -> "go north")
        (verb, args) = ResolveAlias(session, verb, args);

        // Save and update context
        var oldVerb = context.CurrentVerb;
        var oldArgs = context.CurrentArgs;
        var oldNotifyFail = context.NotifyFailMessage;

        context.CurrentVerb = verb;
        context.CurrentArgs = args;
        context.NotifyFailMessage = null;

        try
        {
            // Run command resolution
            // For simplicity, we'll check if any action handles it or if /cmds/ handles it

            // Protected commands
            if (ProtectedCommands.Contains(verb))
            {
                return ExecuteCmdFile(session, verb, args);
            }

            // Core commands with override check
            if (CoreCommands.Contains(verb))
            {
                var overrideAction = FindActionWithOverride(session.PlayerObject, verb);
                if (overrideAction != null && TryExecuteAction(context, session, overrideAction, args))
                {
                    return true;
                }
                if (ExecuteCmdFile(session, verb, args))
                {
                    return true;
                }
            }

            // add_action handlers
            var actions = CollectActions(session.PlayerObject, verb);
            foreach (var action in actions)
            {
                if (TryExecuteAction(context, session, action, args))
                {
                    return true;
                }
            }

            // Fallback to /cmds/
            if (!CoreCommands.Contains(verb))
            {
                return ExecuteCmdFile(session, verb, args);
            }

            return false;
        }
        finally
        {
            // Restore context
            context.CurrentVerb = oldVerb;
            context.CurrentArgs = oldArgs;
            context.NotifyFailMessage = oldNotifyFail;
        }
    }

    /// <summary>
    /// Try to execute a /cmds/ file for the given verb.
    /// Searches command directories based on access level:
    /// - /cmds/admin/ (Admin only)
    /// - /cmds/wizard/ (Wizard+)
    /// - /cmds/std/ (all players)
    /// Returns true if the command file was found and executed.
    /// </summary>
    private bool ExecuteCmdFile(PlayerSession session, string verb, string args)
    {
        // Build list of directories to search based on access level
        var searchPaths = new List<string>();

        if (session.AccessLevel >= AccessLevel.Admin)
        {
            searchPaths.Add($"/cmds/admin/{verb}");
        }
        if (session.AccessLevel >= AccessLevel.Wizard)
        {
            searchPaths.Add($"/cmds/wizard/{verb}");
        }
        searchPaths.Add($"/cmds/std/{verb}");

        // Try each path in order
        MudObject? cmdObj = null;
        foreach (var path in searchPaths)
        {
            try
            {
                cmdObj = _objectManager.LoadObject(path);
                break; // Found it
            }
            catch (ObjectManagerException)
            {
                // Try next path
            }
        }

        if (cmdObj == null)
        {
            return false; // Command not found in any directory
        }

        var mainFunc = cmdObj.FindFunction("main");
        if (mainFunc == null)
        {
            SendToPlayer(session.ConnectionId, $"Command '{verb}' has no main() function.\r\n");
            return true; // We found the file but it's malformed - still "handled"
        }

        try
        {
            _interpreter?.ResetInstructionCount();
            _interpreter?.CallFunctionOnObject(cmdObj, "main", new List<object> { args });
        }
        catch (ReturnException)
        {
            // Normal return from command
        }
        catch (ExecutionLimitException ex)
        {
            SendToPlayer(session.ConnectionId, $"EXECUTION ABORTED: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            SendToPlayer(session.ConnectionId, $"Error: {ex.Message}\r\n");
        }

        return true;
    }

    /// <summary>
    /// Find an action that can override core commands (has OverrideCore flag).
    /// </summary>
    private MudObject.ActionEntry? FindActionWithOverride(MudObject? player, string verb)
    {
        if (player == null) return null;

        var actions = CollectActions(player, verb);
        return actions.FirstOrDefault(a =>
            (a.Flags & MudObject.ActionFlags.OverrideCore) != 0);
    }

    /// <summary>
    /// Collect all action handlers for a verb from relevant objects.
    /// Searches: player inventory, room, room contents.
    /// </summary>
    private List<MudObject.ActionEntry> CollectActions(MudObject? player, string verb)
    {
        var actions = new List<MudObject.ActionEntry>();
        if (player == null) return actions;

        // Check player's own actions (if any)
        var playerAction = player.FindAction(verb);
        if (playerAction != null)
        {
            actions.Add(playerAction);
        }

        // Check inventory
        foreach (var item in player.Contents)
        {
            if (item.IsDestructed) continue;
            var action = item.FindAction(verb);
            if (action != null)
            {
                actions.Add(action);
            }
        }

        // Check room
        var room = player.Environment;
        if (room != null && !room.IsDestructed)
        {
            var roomAction = room.FindAction(verb);
            if (roomAction != null)
            {
                actions.Add(roomAction);
            }

            // Check room contents (other objects in the room)
            foreach (var obj in room.Contents)
            {
                if (obj == player || obj.IsDestructed) continue;
                var action = obj.FindAction(verb);
                if (action != null)
                {
                    actions.Add(action);
                }
            }
        }

        return actions;
    }

    /// <summary>
    /// Try to execute an action handler.
    /// Returns true if the action handled the command (returned non-zero).
    /// </summary>
    private bool TryExecuteAction(ExecutionContext context, PlayerSession session, MudObject.ActionEntry action, string args)
    {
        if (_interpreter == null) return false;

        var target = action.Owner;
        if (target.IsDestructed) return false;

        // Check if the function exists
        if (target.FindFunction(action.Function) == null)
        {
            Logger.Warning($"add_action warning: Function {action.Function} not found on {target.ObjectName}", LogCategory.LPC);
            return false;
        }

        try
        {
            _interpreter.ResetInstructionCount();
            var result = _interpreter.CallFunctionOnObject(target, action.Function, new List<object> { args });

            // If function returns non-zero (truthy), command was handled
            if (result is int i && i != 0)
            {
                return true;
            }
            if (result is long l && l != 0)
            {
                return true;
            }
            if (result is bool b && b)
            {
                return true;
            }
        }
        catch (ReturnException ex)
        {
            // Check the return value
            if (ex.Value is int i && i != 0)
            {
                return true;
            }
            if (ex.Value is long l && l != 0)
            {
                return true;
            }
            if (ex.Value is bool b && b)
            {
                return true;
            }
        }
        catch (ExecutionLimitException ex)
        {
            SendToPlayer(session.ConnectionId, $"EXECUTION ABORTED: {ex.Message}\r\n");
            return true; // Prevent further handlers from running
        }
        catch (Exception ex)
        {
            SendToPlayer(session.ConnectionId, $"Error in {action.Function}: {ex.Message}\r\n");
            return true; // Prevent further handlers from running
        }

        return false;
    }

    /// <summary>
    /// Send a prompt to a player.
    /// </summary>
    private void SendPrompt(string connectionId)
    {
        SendToPlayer(connectionId, "> ");
    }

    #region Login State Machine

    /// <summary>
    /// Process input during login/registration flow.
    /// </summary>
    private void ProcessLoginInput(PlayerSession session, string input)
    {
        input = input.Trim();

        switch (session.LoginState)
        {
            case LoginState.AwaitingName:
                HandleAwaitingName(session, input);
                break;

            case LoginState.AwaitingPassword:
                HandleAwaitingPassword(session, input);
                break;

            case LoginState.RegistrationName:
                HandleRegistrationName(session, input);
                break;

            case LoginState.RegistrationEmail:
                HandleRegistrationEmail(session, input);
                break;

            case LoginState.RegistrationPassword:
                HandleRegistrationPassword(session, input);
                break;

            case LoginState.RegistrationConfirm:
                HandleRegistrationConfirm(session, input);
                break;

            case LoginState.ConfirmTakeover:
                HandleConfirmTakeover(session, input);
                break;

            default:
                // Shouldn't happen, but reset to awaiting name
                session.LoginState = LoginState.AwaitingName;
                SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
                break;
        }
    }

    private void HandleAwaitingName(PlayerSession session, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
            return;
        }

        if (input.Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            session.LoginState = LoginState.RegistrationName;
            SendToPlayer(session.ConnectionId, "\r\nChoose a username (letters only, 3-20 characters): ");
            return;
        }

        // Check if account exists
        if (!_accountManager.AccountExists(input))
        {
            SendToPlayer(session.ConnectionId, "Unknown user. Type 'new' to create a new character.\r\n");
            SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
            return;
        }

        session.PendingUsername = input.ToLowerInvariant();
        session.LoginState = LoginState.AwaitingPassword;

        // Suppress echo for password
        OnSetEchoMode?.Invoke(session.ConnectionId, false);
        SendToPlayer(session.ConnectionId, "Password: ");
    }

    private void HandleAwaitingPassword(PlayerSession session, string input)
    {
        // Restore echo
        OnSetEchoMode?.Invoke(session.ConnectionId, true);
        SendToPlayer(session.ConnectionId, "\r\n");

        // Check for login lockout
        if (_rateLimiter.IsLoginLockedOut(session.ConnectionId))
        {
            var remaining = _rateLimiter.GetLoginLockoutRemaining(session.ConnectionId);
            SendToPlayer(session.ConnectionId, $"Too many failed login attempts. Please wait {remaining} seconds.\r\n\r\n");
            session.PendingUsername = null;
            session.LoginState = LoginState.AwaitingName;
            SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
            return;
        }

        if (_accountManager.ValidateCredentials(session.PendingUsername!, input))
        {
            session.AuthenticatedUsername = session.PendingUsername;
            _accountManager.UpdateLastLogin(session.AuthenticatedUsername!);

            // Clear login attempts on success
            _rateLimiter.ClearLoginAttempts(session.ConnectionId);

            // Check for existing session before completing login
            if (CheckAndPromptForExistingSession(session))
            {
                // Duplicate exists, waiting for confirmation
                return;
            }

            CompleteLogin(session);
        }
        else
        {
            // Record failed attempt
            _rateLimiter.RecordLoginAttempt(session.ConnectionId);

            SendToPlayer(session.ConnectionId, "Invalid password.\r\n\r\n");
            session.PendingUsername = null;
            session.LoginState = LoginState.AwaitingName;
            SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
        }
    }

    private void HandleRegistrationName(PlayerSession session, string input)
    {
        if (!IsValidUsername(input))
        {
            SendToPlayer(session.ConnectionId, "Invalid username. Use 3-20 letters only.\r\n");
            SendToPlayer(session.ConnectionId, "Choose a username: ");
            return;
        }

        // "new" is reserved for the registration command
        if (input.Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            SendToPlayer(session.ConnectionId, "That name is reserved.\r\n");
            SendToPlayer(session.ConnectionId, "Choose a username: ");
            return;
        }

        if (_accountManager.AccountExists(input))
        {
            SendToPlayer(session.ConnectionId, "That name is already taken.\r\n");
            SendToPlayer(session.ConnectionId, "Choose a username: ");
            return;
        }

        session.PendingUsername = input.ToLowerInvariant();
        session.LoginState = LoginState.RegistrationEmail;
        SendToPlayer(session.ConnectionId, "Enter your email address: ");
    }

    private void HandleRegistrationEmail(PlayerSession session, string input)
    {
        // Basic email validation
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('@'))
        {
            SendToPlayer(session.ConnectionId, "Please enter a valid email address: ");
            return;
        }

        session.PendingEmail = input;
        session.LoginState = LoginState.RegistrationPassword;

        OnSetEchoMode?.Invoke(session.ConnectionId, false);
        SendToPlayer(session.ConnectionId, "Choose a password (8+ characters): ");
    }

    private void HandleRegistrationPassword(PlayerSession session, string input)
    {
        if (input.Length < 8)
        {
            SendToPlayer(session.ConnectionId, "\r\nPassword must be at least 8 characters.\r\n");
            SendToPlayer(session.ConnectionId, "Choose a password: ");
            return;
        }

        session.PendingPassword = input;
        session.LoginState = LoginState.RegistrationConfirm;
        SendToPlayer(session.ConnectionId, "\r\nConfirm password: ");
    }

    private void HandleRegistrationConfirm(PlayerSession session, string input)
    {
        OnSetEchoMode?.Invoke(session.ConnectionId, true);
        SendToPlayer(session.ConnectionId, "\r\n");

        if (input != session.PendingPassword)
        {
            SendToPlayer(session.ConnectionId, "Passwords do not match. Let's try again.\r\n\r\n");
            session.PendingPassword = null;
            session.LoginState = LoginState.RegistrationPassword;
            OnSetEchoMode?.Invoke(session.ConnectionId, false);
            SendToPlayer(session.ConnectionId, "Choose a password (8+ characters): ");
            return;
        }

        // Create the account
        if (_accountManager.CreateAccount(session.PendingUsername!, session.PendingEmail!, session.PendingPassword!))
        {
            // Clear sensitive data
            session.PendingPassword = null;

            SendToPlayer(session.ConnectionId, "\r\nAccount created successfully!\r\n\r\n");
            session.AuthenticatedUsername = session.PendingUsername;
            CompleteLogin(session);
        }
        else
        {
            session.PendingPassword = null;
            SendToPlayer(session.ConnectionId, "Failed to create account. Please try again.\r\n\r\n");
            session.LoginState = LoginState.AwaitingName;
            SendToPlayer(session.ConnectionId, "Enter your name, or type 'new': ");
        }
    }

    /// <summary>
    /// Complete the login process and enter the game.
    /// Duplicate session handling should be done BEFORE calling this method.
    /// </summary>
    private void CompleteLogin(PlayerSession session)
    {
        session.LoginState = LoginState.Authenticated;

        try
        {
            // Cache access level from account (outside lock - read-only)
            session.AccessLevel = _accountManager.GetAccessLevel(session.AuthenticatedUsername!);

            // Load aliases from account (outside lock - read-only)
            session.Aliases = _accountManager.GetAliases(session.AuthenticatedUsername!);

            // Clone a player object (outside lock - object creation doesn't need login serialization)
            var playerObject = _objectManager.CloneObject("/std/player");
            playerObject.IsInteractive = true;
            playerObject.ConnectionId = session.ConnectionId;

            // Critical section: atomically check for duplicate and mark as Playing
            // This lock is MINIMAL - only covers the state transition
            lock (_loginLock)
            {
                // Final safety check: if another session snuck in while we were setting up
                var existingSession = FindSessionByUsername(session.AuthenticatedUsername!);
                if (existingSession != null && existingSession.ConnectionId != session.ConnectionId)
                {
                    // Rare race condition: another session completed login while we were setting up
                    // Clean up our player object and abort
                    playerObject.IsInteractive = false;
                    _objectManager.DestructObject(playerObject);
                    SendToPlayer(session.ConnectionId, "Another session has connected. Please try again.\r\n");
                    OnPlayerDisconnect?.Invoke(session.ConnectionId);
                    return;
                }

                // Mark session as Playing (this is what FindSessionByUsername looks for)
                session.PlayerObject = playerObject;
                session.LoginState = LoginState.Playing;
            }
            // Lock released - all remaining operations are safe without synchronization

            // Set player name
            if (_interpreter != null && playerObject.FindFunction("set_name") != null)
            {
                _interpreter.CallFunctionOnObject(playerObject, "set_name",
                    new List<object> { Capitalize(session.AuthenticatedUsername!) });
            }

            // Restore saved player data (must be after set_name so it knows where to look)
            if (_interpreter != null && playerObject.FindFunction("restore_player") != null)
            {
                try
                {
                    _interpreter.ResetInstructionCount();
                    _interpreter.CallFunctionOnObject(playerObject, "restore_player", new List<object>());
                }
                catch (ReturnException)
                {
                    // Normal return
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Could not restore player data: {ex.Message}", LogCategory.Player);
                }
            }

            // Load starting room
            MudObject? startingRoom = null;
            try
            {
                startingRoom = _objectManager.LoadObject(StartingRoomPath);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not load starting room: {ex.Message}", LogCategory.Object);
            }

            // Move player to starting room
            if (startingRoom != null)
            {
                playerObject.MoveTo(startingRoom);
            }

            // Show access level for non-player levels
            var accessMsg = session.AccessLevel switch
            {
                AccessLevel.Admin => " [Admin]",
                AccessLevel.Wizard => " [Wizard]",
                _ => ""
            };
            SendToPlayer(session.ConnectionId, $"Welcome, {Capitalize(session.AuthenticatedUsername!)}{accessMsg}!\r\n\r\n");

            // Execute look command to show the room
            ExecuteLookForPlayer(session);

            SendPrompt(session.ConnectionId);

            Logger.Info($"Player {session.AuthenticatedUsername} logged in from {session.ConnectionId}", LogCategory.Player);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error completing login: {ex.Message}", LogCategory.Player);
            SendToPlayer(session.ConnectionId, "Error entering game. Please reconnect.\r\n");
            OnPlayerDisconnect?.Invoke(session.ConnectionId);
        }
    }

    /// <summary>
    /// Execute the look command for a player who just logged in.
    /// </summary>
    private void ExecuteLookForPlayer(PlayerSession session)
    {
        if (session.PlayerObject == null) return;

        var context = new ExecutionContext
        {
            PlayerObject = session.PlayerObject,
            ConnectionId = session.ConnectionId,
            OutputQueue = _outputQueue,
            CurrentVerb = "look",
            CurrentArgs = ""
        };

        context.Execute(() =>
        {
            ExecuteCmdFile(session, "look", "");
        });
    }

    private static bool IsValidUsername(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length < 3 || name.Length > 20) return false;
        return name.All(char.IsLetter);
    }

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s[1..].ToLower();
    }

    #endregion
}
