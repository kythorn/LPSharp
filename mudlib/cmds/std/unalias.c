// /cmds/std/unalias.c - Remove a command alias
//
// Usage: unalias <name>

int main(string args)
{
    args = trim(args);

    if (args == "")
    {
        write("Usage: unalias <name>\n");
        write("Use 'alias' to list all aliases.\n");
        return 1;
    }

    if (remove_alias(args))
    {
        write(sprintf("Alias '%s' removed.\n", args));
    }
    else
    {
        write(sprintf("No alias '%s' found.\n", args));
    }

    return 1;
}
