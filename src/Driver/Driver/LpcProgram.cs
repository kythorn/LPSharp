namespace Driver;

/// <summary>
/// Represents compiled LPC code (AST + metadata).
/// Programs are shared across all clones of the same blueprint.
/// </summary>
public class LpcProgram
{
    /// <summary>
    /// File path this program was compiled from (without .c extension).
    /// Example: "/std/weapon", "/obj/weapons/sword"
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Programs this program inherits from (in order of inheritance).
    /// Populated when 'inherit' statements are processed.
    /// </summary>
    public List<LpcProgram> InheritedPrograms { get; } = new();

    /// <summary>
    /// Functions defined in this program (not including inherited functions).
    /// Key: function name, Value: function AST node
    /// </summary>
    public Dictionary<string, FunctionDefinition> Functions { get; } = new();

    /// <summary>
    /// Variable declarations in this program (not including inherited variables).
    /// This defines what variables should be created when an object is instantiated.
    /// </summary>
    public List<string> VariableNames { get; } = new();

    /// <summary>
    /// The parsed AST of the program (for potential future use).
    /// Currently null as we'll build this up incrementally.
    /// </summary>
    public Statement? Ast { get; set; }

    /// <summary>
    /// Source code text (for debugging/hot-reload).
    /// </summary>
    public string? SourceCode { get; set; }

    /// <summary>
    /// Time this program was compiled (for hot-reload tracking).
    /// </summary>
    public DateTime CompiledAt { get; set; }

    public LpcProgram(string filePath)
    {
        FilePath = filePath;
        CompiledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Finds a function in this program or its inheritance chain.
    /// Searches depth-first through inherited programs.
    /// </summary>
    /// <param name="name">Function name to find</param>
    /// <returns>Function definition or null if not found</returns>
    public FunctionDefinition? FindFunction(string name)
    {
        // Check this program first
        if (Functions.TryGetValue(name, out var func))
        {
            return func;
        }

        // Check inherited programs in order
        foreach (var inherited in InheritedPrograms)
        {
            var result = inherited.FindFunction(name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a function in this program or its inheritance chain,
    /// returning both the function and the program it was defined in.
    /// This is needed for correct :: (parent call) behavior.
    /// </summary>
    /// <param name="name">Function name to find</param>
    /// <returns>Tuple of (function, owning program) or (null, null) if not found</returns>
    public (FunctionDefinition? Function, LpcProgram? OwningProgram) FindFunctionWithProgram(string name)
    {
        // Check this program first
        if (Functions.TryGetValue(name, out var func))
        {
            return (func, this);
        }

        // Check inherited programs in order
        foreach (var inherited in InheritedPrograms)
        {
            var result = inherited.FindFunctionWithProgram(name);
            if (result.Function != null)
            {
                return result;
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Finds a function in parent programs only (for :: operator).
    /// </summary>
    /// <param name="name">Function name to find</param>
    /// <returns>Function definition or null if not found</returns>
    public FunctionDefinition? FindParentFunction(string name)
    {
        // Only search inherited programs, not this one
        foreach (var inherited in InheritedPrograms)
        {
            var result = inherited.FindFunction(name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all variable names including inherited ones.
    /// Used when initializing a new object instance.
    /// </summary>
    /// <returns>List of all variable names in inheritance order</returns>
    public List<string> GetAllVariableNames()
    {
        var allVars = new List<string>();

        // Add inherited variables first (parents before children)
        foreach (var inherited in InheritedPrograms)
        {
            allVars.AddRange(inherited.GetAllVariableNames());
        }

        // Add this program's variables
        allVars.AddRange(VariableNames);

        return allVars;
    }

    public override string ToString() => $"LpcProgram({FilePath})";
}
