using System.Collections.Concurrent;

namespace Driver;

/// <summary>
/// Thread-local execution context for tracking the current player during LPC execution.
/// Enables this_player() efun and output routing to the correct connection.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// The current execution context for this thread.
    /// ThreadStatic ensures each thread has its own context.
    /// </summary>
    [ThreadStatic]
    private static ExecutionContext? _current;

    /// <summary>
    /// Temporary context used for init() calls outside of command execution.
    /// </summary>
    [ThreadStatic]
    private static ExecutionContext? _initContext;

    /// <summary>
    /// Gets or sets the current execution context for this thread.
    /// </summary>
    public static ExecutionContext? Current
    {
        get => _initContext ?? _current;
        set => _current = value;
    }

    /// <summary>
    /// Set a temporary context for init() calls.
    /// Used when move_object is called outside of a command context.
    /// </summary>
    public static void SetCurrentForInit(MudObject playerObject)
    {
        _initContext = new ExecutionContext { _playerObject = playerObject };
    }

    /// <summary>
    /// Clear the temporary init() context.
    /// </summary>
    public static void ClearCurrentForInit()
    {
        _initContext = null;
    }

    /// <summary>
    /// The player object executing the current command.
    /// Can be temporarily overridden for init() calls.
    /// </summary>
    private MudObject? _playerObject;

    /// <summary>
    /// The player object for this context.
    /// </summary>
    public MudObject? PlayerObject
    {
        get => _playerObject;
        init => _playerObject = value;
    }

    /// <summary>
    /// Temporarily override the player object for init() calls.
    /// </summary>
    public void SetPlayerObjectForInit(MudObject? obj)
    {
        _playerObject = obj;
    }

    /// <summary>
    /// The connection ID associated with this execution context.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The output queue for sending messages back to the connection.
    /// </summary>
    public ConcurrentQueue<OutputMessage>? OutputQueue { get; init; }

    /// <summary>
    /// The current command verb being processed.
    /// Set by command resolution, used by query_verb().
    /// </summary>
    public string? CurrentVerb { get; set; }

    /// <summary>
    /// The current command arguments being processed.
    /// </summary>
    public string? CurrentArgs { get; set; }

    /// <summary>
    /// The failure message to display if no action handles the command.
    /// Set by notify_fail(), cleared between commands.
    /// </summary>
    public string? NotifyFailMessage { get; set; }

    /// <summary>
    /// Send output to the player's connection.
    /// </summary>
    public void SendOutput(string message)
    {
        if (OutputQueue != null && !string.IsNullOrEmpty(ConnectionId))
        {
            OutputQueue.Enqueue(new OutputMessage
            {
                ConnectionId = ConnectionId,
                Content = message
            });
        }
    }

    /// <summary>
    /// Execute an action within this execution context.
    /// Temporarily sets this context as the current context.
    /// </summary>
    public T Execute<T>(Func<T> action)
    {
        var previous = _current;
        _current = this;
        try
        {
            return action();
        }
        finally
        {
            _current = previous;
        }
    }

    /// <summary>
    /// Execute an action within this execution context.
    /// Temporarily sets this context as the current context.
    /// </summary>
    public void Execute(Action action)
    {
        var previous = _current;
        _current = this;
        try
        {
            action();
        }
        finally
        {
            _current = previous;
        }
    }
}

/// <summary>
/// A command queued from a player connection for processing.
/// </summary>
public record PlayerCommand
{
    /// <summary>
    /// The connection ID that submitted this command.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The raw input from the player.
    /// </summary>
    public string Input { get; init; } = string.Empty;

    /// <summary>
    /// When the command was submitted.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// An output message to be sent to a player connection.
/// </summary>
public record OutputMessage
{
    /// <summary>
    /// The connection ID to send this message to.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The content to send.
    /// </summary>
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Tracks a player's session in the game.
/// </summary>
public class PlayerSession
{
    /// <summary>
    /// The connection ID for this session.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The player object for this session.
    /// Null until authentication is complete.
    /// </summary>
    public MudObject? PlayerObject { get; set; }

    /// <summary>
    /// When this session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this session last had activity.
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Pending input handler (set by input_to efun).
    /// If set, next input line goes to this handler instead of command parser.
    /// </summary>
    public InputHandler? PendingInputHandler { get; set; }

    /// <summary>
    /// Current state in the login flow.
    /// </summary>
    public LoginState LoginState { get; set; } = LoginState.Welcome;

    /// <summary>
    /// Username being registered or logged in (before player object exists).
    /// </summary>
    public string? PendingUsername { get; set; }

    /// <summary>
    /// Email being registered (before account created).
    /// </summary>
    public string? PendingEmail { get; set; }

    /// <summary>
    /// Password being registered (cleared after use).
    /// </summary>
    public string? PendingPassword { get; set; }

    /// <summary>
    /// Authenticated username after successful login.
    /// </summary>
    public string? AuthenticatedUsername { get; set; }

    /// <summary>
    /// Cached access level from account data.
    /// Set during login, used for permission checks.
    /// </summary>
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Guest;
}

/// <summary>
/// Handler for capturing player input via input_to().
/// </summary>
public record InputHandler
{
    /// <summary>
    /// The object to call the function on.
    /// </summary>
    public required MudObject Target { get; init; }

    /// <summary>
    /// The function name to call with the input.
    /// </summary>
    public required string Function { get; init; }

    /// <summary>
    /// Flags controlling input behavior.
    /// 0 = normal
    /// 1 = no echo (for passwords)
    /// </summary>
    public int Flags { get; init; } = 0;
}
