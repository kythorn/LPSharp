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
    /// Gets or sets the current execution context for this thread.
    /// </summary>
    public static ExecutionContext? Current
    {
        get => _current;
        set => _current = value;
    }

    /// <summary>
    /// The player object executing the current command.
    /// </summary>
    public MudObject? PlayerObject { get; init; }

    /// <summary>
    /// The connection ID associated with this execution context.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The output queue for sending messages back to the connection.
    /// </summary>
    public ConcurrentQueue<OutputMessage>? OutputQueue { get; init; }

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
public record PlayerSession
{
    /// <summary>
    /// The connection ID for this session.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// The player object for this session.
    /// </summary>
    public MudObject? PlayerObject { get; init; }

    /// <summary>
    /// When this session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this session last had activity.
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}
