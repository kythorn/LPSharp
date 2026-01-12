namespace Driver;

/// <summary>
/// Rate limiter for preventing command flooding and abuse.
/// Uses a sliding window algorithm to track command rates.
/// </summary>
public class RateLimiter
{
    /// <summary>
    /// Maximum commands per window. Default: 30 commands per 10 seconds.
    /// </summary>
    public int MaxCommandsPerWindow { get; set; } = 30;

    /// <summary>
    /// Window size in seconds. Default: 10 seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum failed login attempts before lockout. Default: 5.
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Login lockout duration in seconds. Default: 60 seconds.
    /// </summary>
    public int LoginLockoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether rate limiting is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    // Track command timestamps per connection
    private readonly Dictionary<string, Queue<DateTime>> _commandHistory = new();
    private readonly object _commandLock = new();

    // Track login attempts per IP/connection
    private readonly Dictionary<string, List<DateTime>> _loginAttempts = new();
    private readonly Dictionary<string, DateTime> _loginLockouts = new();
    private readonly object _loginLock = new();

    /// <summary>
    /// Check if a command is allowed for this connection.
    /// Returns true if allowed, false if rate limited.
    /// </summary>
    public bool AllowCommand(string connectionId)
    {
        if (!Enabled)
            return true;

        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-WindowSeconds);

        lock (_commandLock)
        {
            if (!_commandHistory.TryGetValue(connectionId, out var history))
            {
                history = new Queue<DateTime>();
                _commandHistory[connectionId] = history;
            }

            // Remove old entries outside the window
            while (history.Count > 0 && history.Peek() < windowStart)
            {
                history.Dequeue();
            }

            // Check if we're at the limit
            if (history.Count >= MaxCommandsPerWindow)
            {
                return false;
            }

            // Add this command
            history.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Get remaining commands allowed in current window.
    /// </summary>
    public int GetRemainingCommands(string connectionId)
    {
        if (!Enabled)
            return int.MaxValue;

        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-WindowSeconds);

        lock (_commandLock)
        {
            if (!_commandHistory.TryGetValue(connectionId, out var history))
            {
                return MaxCommandsPerWindow;
            }

            // Count commands in window
            int count = 0;
            foreach (var time in history)
            {
                if (time >= windowStart)
                    count++;
            }

            return Math.Max(0, MaxCommandsPerWindow - count);
        }
    }

    /// <summary>
    /// Record a failed login attempt for a connection.
    /// </summary>
    public void RecordLoginAttempt(string connectionId)
    {
        if (!Enabled)
            return;

        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-LoginLockoutSeconds);

        lock (_loginLock)
        {
            if (!_loginAttempts.TryGetValue(connectionId, out var attempts))
            {
                attempts = new List<DateTime>();
                _loginAttempts[connectionId] = attempts;
            }

            // Remove old attempts outside the window
            attempts.RemoveAll(t => t < windowStart);

            // Add this attempt
            attempts.Add(now);

            // Check if we've exceeded the limit
            if (attempts.Count >= MaxLoginAttempts)
            {
                _loginLockouts[connectionId] = now.AddSeconds(LoginLockoutSeconds);
                Logger.Warning($"Login lockout for {connectionId} - too many failed attempts", LogCategory.Network);
            }
        }
    }

    /// <summary>
    /// Check if a connection is locked out from login attempts.
    /// Returns true if locked out.
    /// </summary>
    public bool IsLoginLockedOut(string connectionId)
    {
        if (!Enabled)
            return false;

        lock (_loginLock)
        {
            if (_loginLockouts.TryGetValue(connectionId, out var lockoutUntil))
            {
                if (DateTime.UtcNow < lockoutUntil)
                {
                    return true;
                }

                // Lockout expired, clear it
                _loginLockouts.Remove(connectionId);
            }

            return false;
        }
    }

    /// <summary>
    /// Get time remaining on login lockout in seconds.
    /// Returns 0 if not locked out.
    /// </summary>
    public int GetLoginLockoutRemaining(string connectionId)
    {
        if (!Enabled)
            return 0;

        lock (_loginLock)
        {
            if (_loginLockouts.TryGetValue(connectionId, out var lockoutUntil))
            {
                var remaining = (lockoutUntil - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
            }

            return 0;
        }
    }

    /// <summary>
    /// Clear login attempts after successful login.
    /// </summary>
    public void ClearLoginAttempts(string connectionId)
    {
        lock (_loginLock)
        {
            _loginAttempts.Remove(connectionId);
            _loginLockouts.Remove(connectionId);
        }
    }

    /// <summary>
    /// Remove all tracking for a connection (on disconnect).
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        lock (_commandLock)
        {
            _commandHistory.Remove(connectionId);
        }

        lock (_loginLock)
        {
            _loginAttempts.Remove(connectionId);
            _loginLockouts.Remove(connectionId);
        }
    }

    /// <summary>
    /// Clean up old entries (call periodically).
    /// </summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-WindowSeconds);
        var loginWindowStart = now.AddSeconds(-LoginLockoutSeconds * 2);

        lock (_commandLock)
        {
            var toRemove = new List<string>();
            foreach (var kvp in _commandHistory)
            {
                // Remove old entries
                while (kvp.Value.Count > 0 && kvp.Value.Peek() < windowStart)
                {
                    kvp.Value.Dequeue();
                }

                // Mark empty histories for removal
                if (kvp.Value.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _commandHistory.Remove(key);
            }
        }

        lock (_loginLock)
        {
            // Clean up old login attempts
            var toRemove = new List<string>();
            foreach (var kvp in _loginAttempts)
            {
                kvp.Value.RemoveAll(t => t < loginWindowStart);
                if (kvp.Value.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _loginAttempts.Remove(key);
            }

            // Clean up expired lockouts
            toRemove.Clear();
            foreach (var kvp in _loginLockouts)
            {
                if (kvp.Value < now)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _loginLockouts.Remove(key);
            }
        }
    }
}
