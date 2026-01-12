namespace Driver;

/// <summary>
/// Log severity levels.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    None = 99  // Disable all logging
}

/// <summary>
/// Log categories for filtering and organization.
/// </summary>
public enum LogCategory
{
    General,
    Network,
    Object,
    LPC,
    Player,
    Combat,
    System
}

/// <summary>
/// Central logging system for the driver.
/// Supports log levels, categories, timestamps, and multiple outputs.
/// </summary>
public static class Logger
{
    private static LogLevel _minLevel = LogLevel.Info;
    private static readonly object _lock = new();
    private static StreamWriter? _fileWriter;
    private static string? _logFilePath;
    private static bool _useColors = true;
    private static bool _showTimestamp = true;
    private static bool _showCategory = true;

    /// <summary>
    /// Minimum log level. Messages below this level are ignored.
    /// </summary>
    public static LogLevel MinLevel
    {
        get => _minLevel;
        set => _minLevel = value;
    }

    /// <summary>
    /// Whether to use ANSI colors in console output.
    /// </summary>
    public static bool UseColors
    {
        get => _useColors;
        set => _useColors = value;
    }

    /// <summary>
    /// Whether to show timestamps in log messages.
    /// </summary>
    public static bool ShowTimestamp
    {
        get => _showTimestamp;
        set => _showTimestamp = value;
    }

    /// <summary>
    /// Whether to show category in log messages.
    /// </summary>
    public static bool ShowCategory
    {
        get => _showCategory;
        set => _showCategory = value;
    }

    /// <summary>
    /// Set up file logging. Pass null to disable.
    /// </summary>
    public static void SetLogFile(string? path)
    {
        lock (_lock)
        {
            _fileWriter?.Dispose();
            _fileWriter = null;
            _logFilePath = path;

            if (path != null)
            {
                try
                {
                    // Create directory if needed
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    _fileWriter = new StreamWriter(path, append: true) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to open log file {path}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Close the log file (if any).
    /// </summary>
    public static void Close()
    {
        lock (_lock)
        {
            _fileWriter?.Dispose();
            _fileWriter = null;
        }
    }

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void Debug(string message, LogCategory category = LogCategory.General)
        => Log(LogLevel.Debug, category, message);

    /// <summary>
    /// Log an info message.
    /// </summary>
    public static void Info(string message, LogCategory category = LogCategory.General)
        => Log(LogLevel.Info, category, message);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void Warning(string message, LogCategory category = LogCategory.General)
        => Log(LogLevel.Warning, category, message);

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void Error(string message, LogCategory category = LogCategory.General)
        => Log(LogLevel.Error, category, message);

    /// <summary>
    /// Log a message with explicit level and category.
    /// </summary>
    public static void Log(LogLevel level, LogCategory category, string message)
    {
        if (level < _minLevel)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var categoryStr = category.ToString().PadRight(8);

        // Build message parts
        var parts = new List<string>();
        if (_showTimestamp)
            parts.Add($"[{timestamp}]");
        parts.Add($"[{levelStr}]");
        if (_showCategory)
            parts.Add($"[{categoryStr}]");
        parts.Add(message);

        var fullMessage = string.Join(" ", parts);

        lock (_lock)
        {
            // Write to console with colors
            WriteToConsole(level, fullMessage);

            // Write to file without colors
            _fileWriter?.WriteLine(fullMessage);
        }
    }

    private static void WriteToConsole(LogLevel level, string message)
    {
        if (_useColors)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Parse a log level from string (for command-line args).
    /// </summary>
    public static bool TryParseLevel(string? value, out LogLevel level)
    {
        level = LogLevel.Info;
        if (string.IsNullOrEmpty(value))
            return false;

        return Enum.TryParse(value, ignoreCase: true, out level);
    }
}
