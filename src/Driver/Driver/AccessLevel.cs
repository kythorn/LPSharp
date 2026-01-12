namespace Driver;

/// <summary>
/// Access levels for the MUD. Higher values have more privileges.
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// Guest - during login/registration only. Very restricted.
    /// </summary>
    Guest = 0,

    /// <summary>
    /// Player - standard gameplay, can use /cmds/std/ commands.
    /// </summary>
    Player = 1,

    /// <summary>
    /// Wizard - can clone/destruct objects, load/reload files,
    /// full filesystem access to own home directory (/wizards/{username}/),
    /// read access to public mudlib code.
    /// </summary>
    Wizard = 2,

    /// <summary>
    /// Admin - no restrictions. Full access to all files, commands, and efuns.
    /// </summary>
    Admin = 3
}
