using System.Collections.Concurrent;

namespace Driver;

/// <summary>
/// Central manager for all MUD objects (blueprints and clones).
/// Handles loading, compilation, cloning, and lifecycle management.
/// Singleton pattern - one instance per driver.
/// </summary>
public class ObjectManager
{
    /// <summary>
    /// Blueprints cached by file path (without .c extension).
    /// Thread-safe for concurrent loading from multiple connections.
    /// Key: "/std/weapon", "/obj/weapons/sword"
    /// Value: blueprint MudObject
    /// </summary>
    private readonly ConcurrentDictionary<string, MudObject> _blueprints = new();

    /// <summary>
    /// All objects (blueprints + clones) indexed by full name.
    /// Thread-safe for concurrent lookups.
    /// Key: "/std/weapon", "/obj/weapons/sword#5"
    /// Value: MudObject
    /// </summary>
    private readonly ConcurrentDictionary<string, MudObject> _allObjects = new();

    /// <summary>
    /// Clone counter per blueprint path.
    /// Used to generate unique clone numbers.
    /// Locked during increment to ensure uniqueness.
    /// </summary>
    private readonly Dictionary<string, int> _cloneCounters = new();
    private readonly object _cloneCounterLock = new();

    /// <summary>
    /// Root directory for mudlib files.
    /// All file paths are relative to this.
    /// </summary>
    public string MudlibPath { get; }

    /// <summary>
    /// Dependency tracking: which objects inherit from which.
    /// Key: parent path, Value: list of child paths
    /// Used for future hot-reload implementation.
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _inheritanceChildren = new();
    private readonly object _inheritanceLock = new();

    public ObjectManager(string mudlibPath)
    {
        MudlibPath = Path.GetFullPath(mudlibPath);
    }

    /// <summary>
    /// Load a blueprint object (or get from cache if already loaded).
    /// This is the LPC load_object() efun.
    /// </summary>
    /// <param name="path">Object path without .c extension, e.g. "/std/weapon"</param>
    /// <returns>Blueprint object</returns>
    public MudObject LoadObject(string path)
    {
        // Normalize path (remove .c if present, ensure leading /)
        path = NormalizePath(path);

        // Check cache first (fast path)
        if (_blueprints.TryGetValue(path, out var cached))
        {
            return cached;
        }

        // Not in cache - need to compile
        // Use GetOrAdd for thread-safe lazy loading
        return _blueprints.GetOrAdd(path, p => CompileAndLoad(p));
    }

    /// <summary>
    /// Clone an object from its blueprint.
    /// This is the LPC clone_object() efun.
    /// </summary>
    /// <param name="path">Object path without .c extension</param>
    /// <returns>New clone object</returns>
    public MudObject CloneObject(string path)
    {
        path = NormalizePath(path);

        // Load blueprint (will use cache if available)
        var blueprint = LoadObject(path);

        // Get next clone number (thread-safe)
        int cloneNumber;
        lock (_cloneCounterLock)
        {
            if (!_cloneCounters.ContainsKey(path))
            {
                _cloneCounters[path] = 0;
            }
            cloneNumber = ++_cloneCounters[path];
        }

        // Create clone
        var clone = new MudObject(blueprint, cloneNumber);

        // Register in all objects
        _allObjects[clone.ObjectName] = clone;

        // Call create() on the clone (Milestone 5 - implemented separately)
        // CallCreate(clone);

        return clone;
    }

    /// <summary>
    /// Find an object by its full name (blueprint or clone).
    /// This is the LPC find_object() efun.
    /// </summary>
    /// <param name="name">Full object name, e.g. "/std/weapon" or "/obj/weapons/sword#5"</param>
    /// <returns>Object or null if not found</returns>
    public MudObject? FindObject(string name)
    {
        name = NormalizePath(name);
        _allObjects.TryGetValue(name, out var obj);
        return obj;
    }

    /// <summary>
    /// Destruct an object (remove from game).
    /// This is the LPC destruct() efun.
    /// </summary>
    /// <param name="obj">Object to destruct</param>
    public void DestructObject(MudObject obj)
    {
        if (obj.IsDestructed)
        {
            return; // Already destructed
        }

        // Call dest() lifecycle hook (Milestone 9+)
        // CallDest(obj);

        // Mark as destructed
        obj.IsDestructed = true;

        // Remove from all objects
        _allObjects.TryRemove(obj.ObjectName, out _);

        // If it's a clone, remove from blueprint's clone list
        if (!obj.IsBlueprint && obj.Blueprint != null)
        {
            obj.Blueprint.Clones.Remove(obj);
        }

        // If it's a blueprint, destruct all clones first
        if (obj.IsBlueprint)
        {
            // Copy list to avoid modification during iteration
            var clones = obj.Clones.ToList();
            foreach (var clone in clones)
            {
                DestructObject(clone);
            }

            // Remove from blueprint cache
            _blueprints.TryRemove(obj.FilePath, out _);
        }
    }

    /// <summary>
    /// Compile and load a blueprint from a .c file.
    /// </summary>
    private MudObject CompileAndLoad(string path)
    {
        // Construct full file path
        var fileName = path.TrimStart('/');
        var fullPath = Path.Combine(MudlibPath, fileName + ".c");

        if (!File.Exists(fullPath))
        {
            throw new ObjectManagerException($"File not found: {fullPath}");
        }

        // Read source code
        var sourceCode = File.ReadAllText(fullPath);

        // Compile to program
        var program = CompileProgram(path, sourceCode);

        // Create blueprint object
        var blueprint = new MudObject(program);

        // Register in all objects
        _allObjects[blueprint.ObjectName] = blueprint;

        // Call create() on the blueprint (Milestone 5 - implemented separately)
        // CallCreate(blueprint);

        return blueprint;
    }

    /// <summary>
    /// Compile source code into an LpcProgram.
    /// Handles lexing, parsing, and inheritance resolution.
    /// </summary>
    private LpcProgram CompileProgram(string path, string sourceCode)
    {
        var program = new LpcProgram(path)
        {
            SourceCode = sourceCode
        };

        // Lex
        var lexer = new Lexer(sourceCode);
        var tokens = new List<Token>();
        while (true)
        {
            var token = lexer.NextToken();
            if (token == null)
                break;
            tokens.Add(token);
            if (token.Type == TokenType.Eof)
                break;
        }

        // Parse
        var parser = new Parser(tokens);
        var statements = parser.ParseProgram();

        // Process statements to extract inherits, functions, and variables
        foreach (var stmt in statements)
        {
            if (stmt is InheritStatement inheritStmt)
            {
                // Load the inherited program first (recursive)
                var inheritedBlueprint = LoadObject(inheritStmt.Path);
                program.InheritedPrograms.Add(inheritedBlueprint.Program);

                // Track inheritance for future hot-reload
                TrackInheritance(path, inheritStmt.Path);
            }
            else if (stmt is FunctionDefinition funcDef)
            {
                program.Functions[funcDef.Name] = funcDef;
            }
            else if (stmt is VariableDeclaration varDecl)
            {
                program.VariableNames.Add(varDecl.Name);
            }
        }

        program.Ast = new BlockStatement(statements);

        return program;
    }

    /// <summary>
    /// Normalize an object path:
    /// - Remove .c extension if present
    /// - Ensure leading /
    /// - Handle clone suffix (#123)
    /// </summary>
    private string NormalizePath(string path)
    {
        // Remove .c extension
        if (path.EndsWith(".c"))
        {
            path = path[..^2];
        }

        // Ensure leading /
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path;
    }

    /// <summary>
    /// Track inheritance relationship for future hot-reload support.
    /// </summary>
    private void TrackInheritance(string childPath, string parentPath)
    {
        lock (_inheritanceLock)
        {
            if (!_inheritanceChildren.ContainsKey(parentPath))
            {
                _inheritanceChildren[parentPath] = new HashSet<string>();
            }
            _inheritanceChildren[parentPath].Add(childPath);
        }
    }

    /// <summary>
    /// Get all objects that inherit from a given path.
    /// Used for future hot-reload to find what needs recompiling.
    /// </summary>
    public HashSet<string> GetInheritanceChildren(string path)
    {
        lock (_inheritanceLock)
        {
            if (_inheritanceChildren.TryGetValue(path, out var children))
            {
                return new HashSet<string>(children);
            }
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Get all loaded blueprints (for debugging/admin commands).
    /// </summary>
    public IEnumerable<MudObject> GetAllBlueprints()
    {
        return _blueprints.Values;
    }

    /// <summary>
    /// Get all objects (blueprints + clones) for debugging/admin commands.
    /// </summary>
    public IEnumerable<MudObject> GetAllObjects()
    {
        return _allObjects.Values;
    }

    /// <summary>
    /// Get statistics about the object system.
    /// </summary>
    public ObjectManagerStats GetStats()
    {
        return new ObjectManagerStats
        {
            BlueprintCount = _blueprints.Count,
            TotalObjectCount = _allObjects.Count,
            CloneCount = _allObjects.Values.Count(o => !o.IsBlueprint)
        };
    }
}

/// <summary>
/// Statistics about the object system.
/// </summary>
public record ObjectManagerStats
{
    public int BlueprintCount { get; init; }
    public int TotalObjectCount { get; init; }
    public int CloneCount { get; init; }
}

/// <summary>
/// Exception thrown by ObjectManager.
/// </summary>
public class ObjectManagerException : Exception
{
    public ObjectManagerException(string message) : base(message) { }
    public ObjectManagerException(string message, Exception inner) : base(message, inner) { }
}
