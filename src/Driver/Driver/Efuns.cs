namespace Driver;

/// <summary>
/// Registry for external functions (efuns) callable from LPC code.
/// </summary>
public class EfunRegistry
{
    private readonly Dictionary<string, Func<List<object>, object>> _efuns = new();
    private readonly TextWriter _output;

    /// <summary>
    /// Callback to find session by player object (for tell_room).
    /// Set by GameLoop after initialization.
    /// </summary>
    public static Func<MudObject, PlayerSession?>? FindSessionByPlayer { get; set; }

    /// <summary>
    /// Callback to send output to a player session (for tell_room).
    /// </summary>
    public static Action<string, string>? SendToConnection { get; set; }

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
        Register("this_player", ThisPlayer);
        Register("tell_object", TellObject);
        Register("environment", Environment);
        Register("move_object", MoveObject);
        Register("tell_room", TellRoom);
        Register("all_inventory", AllInventory);
        // Note: load_object, clone_object, etc. are registered by ObjectInterpreter
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
    /// write(value) - Output a value to the player's connection.
    /// Uses ExecutionContext to route output correctly.
    /// Falls back to console output for REPL/tests.
    /// Returns 1.
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

        var context = ExecutionContext.Current;
        if (context != null)
        {
            // Output to player's connection
            context.SendOutput(output + "\r\n");
        }
        else
        {
            // Fallback for REPL/tests
            _output.WriteLine(output);
        }

        return 1;
    }

    /// <summary>
    /// this_player() - Returns the current player object.
    /// Returns 0 if no player is executing (LPC convention).
    /// </summary>
    private static object ThisPlayer(List<object> args)
    {
        if (args.Count != 0)
        {
            throw new EfunException("this_player() takes no arguments");
        }

        var context = ExecutionContext.Current;
        return context?.PlayerObject ?? (object)0;
    }

    /// <summary>
    /// tell_object(target, message) - Send a message to a specific object.
    /// For now, only works if target is the current player.
    /// Returns 1.
    /// </summary>
    private static object TellObject(List<object> args)
    {
        if (args.Count != 2)
        {
            throw new EfunException("tell_object() requires 2 arguments");
        }

        if (args[0] is not MudObject target)
        {
            throw new EfunException("First argument must be an object");
        }

        if (args[1] is not string message)
        {
            throw new EfunException("Second argument must be a string");
        }

        var context = ExecutionContext.Current;
        if (context != null)
        {
            // For MVP: only works if target is the current player
            // TODO Milestone 8: Look up target's connection in GameLoop
            if (target == context.PlayerObject)
            {
                context.SendOutput(message);
            }
        }

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

    /// <summary>
    /// environment(obj) - Returns the environment (container) of an object.
    /// If no argument given, returns environment of this_object().
    /// Returns 0 if object has no environment.
    /// </summary>
    private static object Environment(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            // No arg: use this_object() - but we don't have that yet
            // For now, use this_player()
            var context = ExecutionContext.Current;
            target = context?.PlayerObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is not MudObject obj)
            {
                if (args[0] is int && (int)args[0] == 0)
                {
                    return 0; // LPC convention: environment(0) returns 0
                }
                throw new EfunException("environment() argument must be an object");
            }
            target = obj;
        }
        else
        {
            throw new EfunException("environment() takes 0 or 1 argument");
        }

        return target?.Environment ?? (object)0;
    }

    /// <summary>
    /// move_object(destination) or move_object(what, destination)
    /// Moves an object to a new environment.
    /// Single arg: moves this_object() (or this_player() for now)
    /// Two args: moves first arg to second arg
    /// Returns 1 on success, 0 on failure.
    /// </summary>
    private static object MoveObject(List<object> args)
    {
        MudObject? what;
        MudObject? destination;

        if (args.Count == 1)
        {
            // Single arg: move this_player() to destination
            var context = ExecutionContext.Current;
            what = context?.PlayerObject;

            if (args[0] is not MudObject dest)
            {
                throw new EfunException("move_object() destination must be an object");
            }
            destination = dest;
        }
        else if (args.Count == 2)
        {
            // Two args: move first to second
            if (args[0] is not MudObject obj)
            {
                throw new EfunException("move_object() first argument must be an object");
            }
            what = obj;

            if (args[1] is not MudObject dest)
            {
                throw new EfunException("move_object() destination must be an object");
            }
            destination = dest;
        }
        else
        {
            throw new EfunException("move_object() requires 1 or 2 arguments");
        }

        if (what == null)
        {
            return 0;
        }

        return what.MoveTo(destination) ? 1 : 0;
    }

    /// <summary>
    /// tell_room(room, message) or tell_room(room, message, exclude)
    /// Sends a message to all objects in a room.
    /// Optional third arg is an object to exclude from the message.
    /// Returns 1.
    /// </summary>
    private static object TellRoom(List<object> args)
    {
        if (args.Count < 2 || args.Count > 3)
        {
            throw new EfunException("tell_room() requires 2 or 3 arguments");
        }

        if (args[0] is not MudObject room)
        {
            throw new EfunException("tell_room() first argument must be a room object");
        }

        if (args[1] is not string message)
        {
            throw new EfunException("tell_room() second argument must be a string");
        }

        MudObject? exclude = null;
        if (args.Count == 3 && args[2] is MudObject excl)
        {
            exclude = excl;
        }

        // Send to all players in the room
        foreach (var obj in room.Contents)
        {
            if (obj == exclude)
            {
                continue;
            }

            // Try to find a session for this object and send output
            if (FindSessionByPlayer != null && SendToConnection != null)
            {
                var session = FindSessionByPlayer(obj);
                if (session != null)
                {
                    SendToConnection(session.ConnectionId, message);
                }
            }
            else
            {
                // Fallback: check if it's the current player
                var context = ExecutionContext.Current;
                if (context != null && obj == context.PlayerObject)
                {
                    context.SendOutput(message);
                }
            }
        }

        return 1;
    }

    /// <summary>
    /// all_inventory(obj) - Returns an array of all objects in obj.
    /// If no argument, returns contents of this_object().
    /// Note: Returns a placeholder since we don't have arrays yet.
    /// For now, returns the count of objects.
    /// </summary>
    private static object AllInventory(List<object> args)
    {
        MudObject? target;

        if (args.Count == 0)
        {
            var context = ExecutionContext.Current;
            target = context?.PlayerObject;
        }
        else if (args.Count == 1)
        {
            if (args[0] is not MudObject obj)
            {
                throw new EfunException("all_inventory() argument must be an object");
            }
            target = obj;
        }
        else
        {
            throw new EfunException("all_inventory() takes 0 or 1 argument");
        }

        // For now, return count since we don't have arrays
        // TODO: Return actual array when array support is added
        return target?.Contents.Count ?? 0;
    }

    #endregion
}

public class EfunException : Exception
{
    public EfunException(string message) : base(message) { }
}
