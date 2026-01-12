// /cmds/std/alias.c - Manage command aliases
//
// Usage:
//   alias              - List all aliases
//   alias <name>       - Show definition of an alias
//   alias <name> <cmd> - Set an alias
//   alias -d <name>    - Delete an alias
//   alias --reset      - Reset all aliases to defaults

int main(string args)
{
    string arg;
    mapping aliases;
    string *keys;
    int i;
    int count;

    args = trim(args);

    // No arguments: list all aliases
    if (args == "")
    {
        aliases = query_aliases();
        keys = m_indices(aliases);
        count = sizeof(keys);

        if (count == 0)
        {
            write("You have no aliases defined.\n");
            return 1;
        }

        write("Your aliases:\n");
        for (i = 0; i < count; i++)
        {
            write(sprintf("  %-12s = %s\n", keys[i], aliases[keys[i]]));
        }
        write(sprintf("\nTotal: %d aliases\n", count));
        return 1;
    }

    // Reset aliases
    if (args == "--reset" || args == "-reset" || args == "reset")
    {
        if (reset_aliases())
        {
            write("Aliases reset to defaults.\n");
        }
        else
        {
            write("Failed to reset aliases.\n");
        }
        return 1;
    }

    // Delete alias: alias -d <name>
    if (sscanf(args, "-d %s", arg) == 1 ||
        sscanf(args, "--delete %s", arg) == 1)
    {
        arg = trim(arg);
        if (arg == "")
        {
            write("Usage: alias -d <name>\n");
            return 1;
        }

        if (remove_alias(arg))
        {
            write(sprintf("Alias '%s' removed.\n", arg));
        }
        else
        {
            write(sprintf("No alias '%s' found.\n", arg));
        }
        return 1;
    }

    // Check if setting or showing an alias
    // Find first space to separate alias name from command
    int space = member(args, ' ');

    if (space == -1)
    {
        // Single word: show definition
        arg = query_alias(args);
        if (stringp(arg))
        {
            write(sprintf("%s = %s\n", args, arg));
        }
        else
        {
            write(sprintf("No alias '%s' defined.\n", args));
        }
        return 1;
    }

    // Setting alias: alias <name> <command>
    string name;
    string cmd;
    name = trim(args[0..space-1]);
    cmd = trim(args[space+1..]);

    if (name == "" || cmd == "")
    {
        write("Usage: alias <name> <command>\n");
        return 1;
    }

    // Prevent aliasing 'alias' itself
    if (lower_case(name) == "alias")
    {
        write("Cannot create an alias for 'alias'.\n");
        return 1;
    }

    if (set_alias(name, cmd))
    {
        write(sprintf("Alias '%s' set to '%s'.\n", name, cmd));
    }
    else
    {
        write("Failed to set alias.\n");
    }

    return 1;
}
