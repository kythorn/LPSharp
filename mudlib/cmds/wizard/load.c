// load.c - Load or reload an object
// Usage: load <path>
// Loads the specified object (or reloads if already loaded).
// Supports relative paths.

void main(string args) {
    object player;
    object existing;
    object obj;
    string path;
    int count;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        write("Usage: load <path>");
        write("Example: load /std/object");
        write("Example: load room.c");
        return;
    }

    // Resolve relative path
    path = call_other(player, "resolve_path", args);

    // Check if already loaded
    existing = find_object(path);

    if (existing) {
        // Use update to reload
        count = update(path);
        write("Updated " + path + " (" + count + " object(s) affected)");
    } else {
        // Fresh load
        obj = load_object(path);
        if (obj) {
            write("Loaded: " + object_name(obj));
        } else {
            write("Failed to load: " + path);
        }
    }
}
