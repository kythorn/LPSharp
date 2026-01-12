namespace Driver;

/// <summary>
/// C-style preprocessor for LPC files.
/// Handles #include, #define, #undef, #ifdef, #ifndef, #else, #endif.
/// </summary>
public class Preprocessor
{
    private readonly string _mudlibPath;
    private readonly Dictionary<string, string> _defines = new();
    private readonly HashSet<string> _includedFiles = new();
    private readonly Stack<bool> _conditionStack = new();
    private int _currentLine;
    private string _currentFile = "";

    /// <summary>
    /// Maximum include depth to prevent infinite recursion.
    /// </summary>
    public int MaxIncludeDepth { get; set; } = 20;

    public Preprocessor(string mudlibPath)
    {
        _mudlibPath = mudlibPath;
    }

    /// <summary>
    /// Predefine a symbol.
    /// </summary>
    public void Define(string name, string value = "1")
    {
        _defines[name] = value;
    }

    /// <summary>
    /// Process a source file and return the preprocessed source.
    /// </summary>
    public string Process(string source, string filePath)
    {
        _currentFile = filePath;
        _currentLine = 1;
        _conditionStack.Clear();
        _includedFiles.Clear();

        var result = new System.Text.StringBuilder();
        var lines = source.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#"))
            {
                ProcessDirective(trimmed, result, 0);
            }
            else if (IsOutputEnabled())
            {
                // Apply macro substitution
                result.AppendLine(SubstituteMacros(line));
            }
            else
            {
                // Keep line for line number tracking but empty
                result.AppendLine();
            }

            _currentLine++;
        }

        if (_conditionStack.Count > 0)
        {
            throw new PreprocessorException($"Unterminated #ifdef/#ifndef", _currentFile, _currentLine);
        }

        return result.ToString();
    }

    private void ProcessDirective(string line, System.Text.StringBuilder result, int includeDepth)
    {
        var parts = line.Substring(1).Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var directive = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1].Trim() : "";

        switch (directive)
        {
            case "include":
                ProcessInclude(args, result, includeDepth);
                break;
            case "define":
                if (IsOutputEnabled())
                    ProcessDefine(args);
                result.AppendLine(); // Preserve line number
                break;
            case "undef":
                if (IsOutputEnabled())
                    ProcessUndef(args);
                result.AppendLine(); // Preserve line number
                break;
            case "ifdef":
                ProcessIfdef(args, negate: false);
                result.AppendLine(); // Preserve line number
                break;
            case "ifndef":
                ProcessIfdef(args, negate: true);
                result.AppendLine(); // Preserve line number
                break;
            case "else":
                ProcessElse();
                result.AppendLine(); // Preserve line number
                break;
            case "endif":
                ProcessEndif();
                result.AppendLine(); // Preserve line number
                break;
            default:
                // Unknown directive - just output an empty line
                result.AppendLine();
                break;
        }
    }

    private void ProcessInclude(string args, System.Text.StringBuilder result, int includeDepth)
    {
        if (!IsOutputEnabled())
        {
            result.AppendLine(); // Keep line for numbering
            return;
        }

        if (includeDepth >= MaxIncludeDepth)
        {
            throw new PreprocessorException($"Include depth exceeded ({MaxIncludeDepth})", _currentFile, _currentLine);
        }

        // Extract file path from "file" or <file>
        string includePath;
        if (args.StartsWith("\"") && args.EndsWith("\""))
        {
            includePath = args.Substring(1, args.Length - 2);
        }
        else if (args.StartsWith("<") && args.EndsWith(">"))
        {
            includePath = args.Substring(1, args.Length - 2);
        }
        else
        {
            throw new PreprocessorException($"Invalid #include syntax: {args}", _currentFile, _currentLine);
        }

        // Resolve path
        var resolvedPath = ResolvePath(includePath);

        // Add .c extension if not already present
        if (!resolvedPath.EndsWith(".c"))
        {
            resolvedPath = resolvedPath + ".c";
        }

        var fullPath = Path.Combine(_mudlibPath, resolvedPath.TrimStart('/'));

        // Normalize for duplicate checking
        var normalizedPath = Path.GetFullPath(fullPath);

        // Check for circular includes
        if (_includedFiles.Contains(normalizedPath))
        {
            // Already included - skip to prevent infinite loop
            result.AppendLine($"// Already included: {includePath}");
            return;
        }

        if (!File.Exists(fullPath))
        {
            throw new PreprocessorException($"Include file not found: {includePath}", _currentFile, _currentLine);
        }

        _includedFiles.Add(normalizedPath);

        try
        {
            var includeSource = File.ReadAllText(fullPath);
            var savedFile = _currentFile;
            var savedLine = _currentLine;

            _currentFile = includePath;
            _currentLine = 1;

            // Process the included file
            var includeLines = includeSource.Split('\n');
            foreach (var rawLine in includeLines)
            {
                var incLine = rawLine.TrimEnd('\r');
                var trimmed = incLine.TrimStart();

                if (trimmed.StartsWith("#"))
                {
                    ProcessDirective(trimmed, result, includeDepth + 1);
                }
                else if (IsOutputEnabled())
                {
                    result.AppendLine(SubstituteMacros(incLine));
                }
                else
                {
                    result.AppendLine();
                }

                _currentLine++;
            }

            _currentFile = savedFile;
            _currentLine = savedLine;
        }
        catch (IOException ex)
        {
            throw new PreprocessorException($"Error reading include file {includePath}: {ex.Message}", _currentFile, _currentLine);
        }
    }

    private void ProcessDefine(string args)
    {
        var parts = args.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new PreprocessorException("Missing macro name in #define", _currentFile, _currentLine);
        }

        var name = parts[0];
        var value = parts.Length > 1 ? parts[1] : "1";

        _defines[name] = value;
    }

    private void ProcessUndef(string args)
    {
        var name = args.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new PreprocessorException("Missing macro name in #undef", _currentFile, _currentLine);
        }

        _defines.Remove(name);
    }

    private void ProcessIfdef(string args, bool negate)
    {
        var name = args.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new PreprocessorException($"Missing macro name in #{(negate ? "ifndef" : "ifdef")}", _currentFile, _currentLine);
        }

        var isDefined = _defines.ContainsKey(name);
        var condition = negate ? !isDefined : isDefined;

        // If we're already in a false branch, the nested condition is also false
        if (!IsOutputEnabled())
        {
            _conditionStack.Push(false);
        }
        else
        {
            _conditionStack.Push(condition);
        }
    }

    private void ProcessElse()
    {
        if (_conditionStack.Count == 0)
        {
            throw new PreprocessorException("#else without matching #ifdef/#ifndef", _currentFile, _currentLine);
        }

        // Check if parent condition is enabled (look at second-to-top)
        bool parentEnabled = true;
        if (_conditionStack.Count > 1)
        {
            var temp = _conditionStack.Pop();
            parentEnabled = IsOutputEnabled();
            _conditionStack.Push(temp);
        }

        if (parentEnabled)
        {
            // Flip the top condition
            var current = _conditionStack.Pop();
            _conditionStack.Push(!current);
        }
        // If parent is false, leave it false
    }

    private void ProcessEndif()
    {
        if (_conditionStack.Count == 0)
        {
            throw new PreprocessorException("#endif without matching #ifdef/#ifndef", _currentFile, _currentLine);
        }

        _conditionStack.Pop();
    }

    private bool IsOutputEnabled()
    {
        // Output is enabled if all conditions in the stack are true
        foreach (var cond in _conditionStack)
        {
            if (!cond) return false;
        }
        return true;
    }

    private string SubstituteMacros(string line)
    {
        if (_defines.Count == 0) return line;

        // Simple word-boundary substitution
        foreach (var kvp in _defines)
        {
            // Use word boundaries to avoid replacing partial matches
            var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b";
            line = System.Text.RegularExpressions.Regex.Replace(line, pattern, kvp.Value);
        }

        return line;
    }

    private string ResolvePath(string path)
    {
        // If path starts with /, it's absolute
        if (path.StartsWith("/"))
        {
            return path;
        }

        // Otherwise, resolve relative to current file's directory
        var currentDir = Path.GetDirectoryName(_currentFile)?.Replace('\\', '/') ?? "";
        if (string.IsNullOrEmpty(currentDir))
        {
            return "/" + path;
        }

        // Combine and normalize
        var combined = currentDir + "/" + path;
        return "/" + combined.TrimStart('/');
    }
}

/// <summary>
/// Exception thrown by the preprocessor.
/// </summary>
public class PreprocessorException : Exception
{
    public string File { get; }
    public int Line { get; }

    public PreprocessorException(string message, string file, int line)
        : base($"{message} at {file}:{line}")
    {
        File = file;
        Line = line;
    }
}
