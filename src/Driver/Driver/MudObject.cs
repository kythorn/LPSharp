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
    /// For blueprints: stored directly.
    /// For clones: dynamically fetched from blueprint (allows hot-reload).
    /// </summary>
    public LpcProgram Program => _program ?? Blueprint!.Program;

    /// <summary>
    /// Backing field for Program (only used by blueprints).
    /// </summary>
    private LpcProgram? _program;

    /// <summary>
    /// Update the program for this blueprint (used by hot-reload).
    /// Only valid for blueprints.
    /// </summary>
    public void UpdateProgram(LpcProgram newProgram)
    {
        if (!IsBlueprint)
        {
            throw new InvalidOperationException("Cannot update program on a clone");
        }
        _program = newProgram;
    }

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

    #region Living/Interactive Properties

    /// <summary>
    /// Whether this object is "living" (can have HP, receive heartbeats, etc).
    /// Set via set_living() efun. Typically true for players and NPCs.
    /// </summary>
    public bool IsLiving { get; set; }

    /// <summary>
    /// The "living name" for this object (used by find_living()).
    /// Set via set_living_name() efun. Examples: "orc", "troll", "player_bob"
    /// </summary>
    public string? LivingName { get; set; }

    /// <summary>
    /// Whether this object is an interactive player (connected via telnet).
    /// Set by GameLoop when player connects/disconnects.
    /// </summary>
    public bool IsInteractive { get; set; }

    /// <summary>
    /// The connection ID for interactive players.
    /// Used to route output to the correct telnet session.
    /// </summary>
    public string? ConnectionId { get; set; }

    #endregion

    #region Heartbeat Properties

    /// <summary>
    /// Whether heartbeat is enabled for this object.
    /// When true, heart_beat() is called periodically by the game loop.
    /// </summary>
    public bool HeartbeatEnabled { get; set; }

    /// <summary>
    /// Last time heart_beat() was called on this object.
    /// Used to implement heartbeat frequency.
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reset interval in seconds for this object.
    /// When > 0, reset() is called periodically at this interval.
    /// Set via set_reset() efun.
    /// </summary>
    public int ResetInterval { get; set; }

    #endregion

    #region Shadow System

    /// <summary>
    /// The object that is shadowing this one (outermost shadow in chain).
    /// When a function is called on this object, the shadow gets first chance to intercept.
    /// </summary>
    public MudObject? ShadowedBy { get; set; }

    /// <summary>
    /// The object this is shadowing.
    /// If set, this object intercepts function calls to the shadowed object.
    /// </summary>
    public MudObject? Shadowing { get; set; }

    #endregion

    #region Action System (add_action)

    /// <summary>
    /// Flags for add_action registration.
    /// </summary>
    [Flags]
    public enum ActionFlags
    {
        None = 0,
        /// <summary>Match verb as prefix (e.g., "l" matches "look")</summary>
        MatchPrefix = 1,
        /// <summary>Allow this action to override core commands</summary>
        OverrideCore = 2,
    }

    /// <summary>
    /// An action registered via add_action().
    /// </summary>
    public record ActionEntry(
        string Verb,
        string Function,
        ActionFlags Flags,
        MudObject Owner
    );

    /// <summary>
    /// Actions registered by this object via add_action().
    /// These are actions this object provides to command givers (players).
    /// </summary>
    private readonly List<ActionEntry> _actions = new();

    /// <summary>
    /// Read-only access to registered actions.
    /// </summary>
    public IReadOnlyList<ActionEntry> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Whether this object can receive and process commands.
    /// Set via enable_commands(). Typically true only for players.
    /// </summary>
    public bool CommandsEnabled { get; set; }

    /// <summary>
    /// Register an action handler for a verb.
    /// Called by add_action() efun.
    /// </summary>
    public void AddAction(string function, string verb, ActionFlags flags = ActionFlags.None)
    {
        // Remove any existing action for this verb from this object
        _actions.RemoveAll(a => a.Verb == verb);
        _actions.Add(new ActionEntry(verb, function, flags, this));
    }

    /// <summary>
    /// Remove an action handler for a verb.
    /// </summary>
    public bool RemoveAction(string verb)
    {
        return _actions.RemoveAll(a => a.Verb == verb) > 0;
    }

    /// <summary>
    /// Clear all registered actions.
    /// Called when object leaves an environment.
    /// </summary>
    public void ClearActions()
    {
        _actions.Clear();
    }

    /// <summary>
    /// Find an action that matches the given verb.
    /// </summary>
    public ActionEntry? FindAction(string verb)
    {
        foreach (var action in _actions)
        {
            if (action.Verb == verb)
            {
                return action;
            }
            // Check prefix match
            if ((action.Flags & ActionFlags.MatchPrefix) != 0 && verb.StartsWith(action.Verb))
            {
                return action;
            }
        }
        return null;
    }

    #endregion

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
        _program = program;
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
        _program = null;  // Clones dynamically get Program from Blueprint
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
