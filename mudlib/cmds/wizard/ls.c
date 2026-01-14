// ls.c - List directory contents
// Usage: ls [path]
// If no path given, lists current working directory.
// Supports relative paths.

void main(string args) {
    object player;
    string path;
    mixed *entries;
    int i;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        // Default to current working directory
        path = call_other(player, "query_cwd");
        if (!path || path == "") {
            path = "/";
        }
    } else {
        // Resolve relative path
        path = call_other(player, "resolve_path", args);
    }

    entries = get_dir(path);

    if (!entries || sizeof(entries) == 0) {
        write("No files found or directory does not exist: " + path);
        return;
    }

    write("Contents of " + path + ":");

    for (i = 0; i < sizeof(entries); i++) {
        write("  " + entries[i]);
    }

    write(sizeof(entries) + " item(s)");
}
