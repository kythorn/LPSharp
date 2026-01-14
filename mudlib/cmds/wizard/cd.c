// cd.c - Change current working directory
// Usage: cd [path]
// If no path given, goes to home directory.
// Supports: absolute paths (/foo), relative paths (foo), parent (..)

void main(string args) {
    object player;
    string path;
    string resolved;
    mixed *entries;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        // No argument - go to home directory
        path = homedir();
        if (!path || path == "") {
            path = "/";
        }
    } else {
        path = args;
    }

    // Resolve the path relative to current cwd
    resolved = call_other(player, "resolve_path", path);

    // Check if directory exists by trying to get its contents
    entries = get_dir(resolved);

    if (!entries || sizeof(entries) == 0) {
        // Could be an empty directory or non-existent
        // Try to check if it's a valid directory by checking parent
        string parent;
        string dirname;
        int slash_pos;

        if (resolved == "/") {
            // Root always exists
            call_other(player, "set_cwd", resolved);
            write(resolved);
            return;
        }

        // Check if it exists in parent directory
        slash_pos = member(resolved, '/');
        if (slash_pos >= 0) {
            // Find the last slash
            string *parts;
            parts = explode(resolved, "/");
            if (sizeof(parts) > 0) {
                dirname = parts[sizeof(parts) - 1];
                if (sizeof(parts) > 1) {
                    parent = "/" + implode(parts[0..sizeof(parts)-2], "/");
                } else {
                    parent = "/";
                }

                entries = get_dir(parent);
                int found;
                int i;
                found = 0;
                for (i = 0; i < sizeof(entries); i++) {
                    if (entries[i] == dirname || entries[i] == dirname + "/") {
                        found = 1;
                        break;
                    }
                }

                if (!found) {
                    write("Directory not found: " + resolved);
                    return;
                }
            }
        }
    }

    call_other(player, "set_cwd", resolved);
    write(resolved);
}
