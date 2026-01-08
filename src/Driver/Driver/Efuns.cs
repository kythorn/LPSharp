namespace Driver;

/// <summary>
/// Registry for external functions (efuns) callable from LPC code.
/// </summary>
public class EfunRegistry
{
    private readonly Dictionary<string, Func<List<object>, object>> _efuns = new();
    private readonly TextWriter _output;

    public EfunRegistry(TextWriter? output = null)
    {
        _output = output ?? Console.Out;
        RegisterBuiltins();
    }

    private void RegisterBuiltins()
    {
        Register("write", Write);
        Register("typeof", TypeOf);
        Register("strlen", Strlen);
        Register("to_string", ToString);
        Register("to_int", ToInt);
    }

    public void Register(string name, Func<List<object>, object> implementation)
    {
        _efuns[name] = implementation;
    }

    public bool TryGet(string name, out Func<List<object>, object>? efun)
    {
        return _efuns.TryGetValue(name, out efun);
    }

    public bool Exists(string name) => _efuns.ContainsKey(name);

    #region Built-in Efuns

    /// <summary>
    /// write(value) - Output a value to the console. Returns 1.
    /// </summary>
    private object Write(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("write() requires exactly 1 argument");
        }

        var value = args[0];
        var output = value switch
        {
            string s => s,
            int i => i.ToString(),
            _ => value.ToString() ?? ""
        };

        _output.WriteLine(output);
        return 1;
    }

    /// <summary>
    /// typeof(value) - Returns the type name as a string.
    /// </summary>
    private static object TypeOf(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("typeof() requires exactly 1 argument");
        }

        return args[0] switch
        {
            int => "int",
            string => "string",
            _ => "unknown"
        };
    }

    /// <summary>
    /// strlen(string) - Returns the length of a string.
    /// </summary>
    private static object Strlen(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("strlen() requires exactly 1 argument");
        }

        if (args[0] is not string s)
        {
            throw new EfunException("strlen() requires a string argument");
        }

        return s.Length;
    }

    /// <summary>
    /// to_string(value) - Converts a value to a string.
    /// </summary>
    private static object ToString(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("to_string() requires exactly 1 argument");
        }

        return args[0] switch
        {
            string s => s,
            int i => i.ToString(),
            _ => args[0].ToString() ?? ""
        };
    }

    /// <summary>
    /// to_int(value) - Converts a value to an integer.
    /// </summary>
    private static object ToInt(List<object> args)
    {
        if (args.Count != 1)
        {
            throw new EfunException("to_int() requires exactly 1 argument");
        }

        return args[0] switch
        {
            int i => i,
            string s when int.TryParse(s, out var result) => result,
            string => 0, // LPC returns 0 for non-numeric strings
            _ => throw new EfunException($"to_int() cannot convert {args[0].GetType().Name}")
        };
    }

    #endregion
}

public class EfunException : Exception
{
    public EfunException(string message) : base(message) { }
}
