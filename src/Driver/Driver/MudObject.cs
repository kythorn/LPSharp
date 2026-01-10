namespace Driver;

/// <summary>
/// Represents an LPC object (either a blueprint or a clone).
/// Blueprints are singletons representing compiled .c files.
/// Clones are instances with their own variable state.
/// </summary>
public class MudObject
{
    /// <summary>
    /// The full object name including clone suffix if applicable.
    /// Examples: "/std/weapon", "/obj/weapons/sword#5"
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// The file path this object was loaded from (without .c extension).
    /// Example: "/std/weapon", "/obj/weapons/sword"
    /// This is the same for both blueprints and their clones.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Whether this is a blueprint (master object) or a clone (instance).
    /// </summary>
    public bool IsBlueprint { get; }

    /// <summary>
    /// Clone number for clones (1, 2, 3...), null for blueprints.
    /// </summary>
    public int? CloneNumber { get; }

    /// <summary>
    /// The compiled program this object executes.
    /// Shared between blueprint and all its clones.
    /// </summary>
    public LpcProgram Program { get; }

    /// <summary>
    /// Instance variables for this object.
    /// Each clone has its own independent variable storage.
    /// Blueprints can also have variables (shared state - rarely used).
    /// </summary>
    public Dictionary<string, object?> Variables { get; } = new();

    /// <summary>
    /// For clones: reference to their blueprint.
    /// For blueprints: null.
    /// </summary>
    public MudObject? Blueprint { get; }

    /// <summary>
    /// For blueprints: list of active clones (for tracking/cleanup).
    /// For clones: empty list.
    /// Note: This is a weak reference in real LPMud, but we'll track explicitly for now.
    /// </summary>
    public List<MudObject> Clones { get; } = new();

    /// <summary>
    /// Whether this object has been destructed.
    /// </summary>
    public bool IsDestructed { get; set; }

    /// <summary>
    /// Time this object was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// The environment (container) this object is in.
    /// For players: typically a room.
    /// For items: a room, player, or container.
    /// null if not in any environment.
    /// </summary>
    public MudObject? Environment { get; private set; }

    /// <summary>
    /// Objects contained within this object.
    /// For rooms: players, NPCs, items on the floor.
    /// For players: inventory items.
    /// </summary>
    private readonly List<MudObject> _contents = new();

    /// <summary>
    /// Read-only access to contents.
    /// </summary>
    public IReadOnlyList<MudObject> Contents => _contents.AsReadOnly();

    /// <summary>
    /// Create a blueprint object.
    /// </summary>
    public MudObject(LpcProgram program)
    {
        FilePath = program.FilePath;
        ObjectName = program.FilePath;
        IsBlueprint = true;
        CloneNumber = null;
        Program = program;
        Blueprint = null;
        CreatedAt = DateTime.UtcNow;

        // Initialize variables for blueprint
        InitializeVariables();
    }

    /// <summary>
    /// Create a clone object.
    /// </summary>
    public MudObject(MudObject blueprint, int cloneNumber)
    {
        if (!blueprint.IsBlueprint)
        {
            throw new InvalidOperationException("Cannot clone from a non-blueprint object");
        }

        FilePath = blueprint.FilePath;
        ObjectName = $"{blueprint.FilePath}#{cloneNumber}";
        IsBlueprint = false;
        CloneNumber = cloneNumber;
        Program = blueprint.Program;
        Blueprint = blueprint;
        CreatedAt = DateTime.UtcNow;

        // Initialize variables for clone (independent copy)
        InitializeVariables();

        // Register this clone with the blueprint
        blueprint.Clones.Add(this);
    }

    /// <summary>
    /// Initialize variables from the program's variable declarations.
    /// Creates entries for all variables (including inherited ones).
    /// </summary>
    private void InitializeVariables()
    {
        var allVars = Program.GetAllVariableNames();
        foreach (var varName in allVars)
        {
            // Initialize to 0 (LPC default for uninitialized variables)
            // Future: Could use type information for proper defaults
            Variables[varName] = 0;
        }
    }

    /// <summary>
    /// Get a variable value.
    /// </summary>
    public object? GetVariable(string name)
    {
        if (Variables.TryGetValue(name, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"Variable '{name}' not found in object {ObjectName}");
    }

    /// <summary>
    /// Set a variable value.
    /// </summary>
    public void SetVariable(string name, object? value)
    {
        if (Variables.ContainsKey(name))
        {
            Variables[name] = value;
        }
        else
        {
            throw new InvalidOperationException($"Variable '{name}' not found in object {ObjectName}");
        }
    }

    /// <summary>
    /// Find a function in this object's program (including inherited).
    /// </summary>
    public FunctionDefinition? FindFunction(string name)
    {
        return Program.FindFunction(name);
    }

    /// <summary>
    /// Find a parent function (for :: operator support).
    /// </summary>
    public FunctionDefinition? FindParentFunction(string name)
    {
        return Program.FindParentFunction(name);
    }

    public override string ToString()
    {
        var type = IsBlueprint ? "Blueprint" : "Clone";
        return $"{type}({ObjectName})";
    }

    /// <summary>
    /// Move this object into a new environment.
    /// Removes from old environment (if any), adds to new environment.
    /// </summary>
    /// <param name="destination">The new environment, or null to remove from any environment</param>
    /// <returns>True if move succeeded</returns>
    public bool MoveTo(MudObject? destination)
    {
        // Can't move into self
        if (destination == this)
        {
            return false;
        }

        // Remove from old environment
        Environment?._contents.Remove(this);
        var oldEnvironment = Environment;
        Environment = null;

        // Add to new environment
        if (destination != null)
        {
            destination._contents.Add(this);
            Environment = destination;
        }

        return true;
    }

    /// <summary>
    /// Check if this object contains another object (directly or indirectly).
    /// Used to prevent circular containment.
    /// </summary>
    public bool Contains(MudObject obj)
    {
        if (_contents.Contains(obj))
        {
            return true;
        }

        foreach (var content in _contents)
        {
            if (content.Contains(obj))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get a display name for debugging.
    /// </summary>
    public string GetDisplayName()
    {
        if (IsBlueprint)
        {
            return $"{ObjectName} [blueprint, {Clones.Count} clones]";
        }
        else
        {
            return $"{ObjectName} [clone]";
        }
    }
}
