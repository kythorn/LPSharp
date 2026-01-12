// load.c - Load or reload an object
// Usage: load <path>
// Loads the specified object (or reloads if already loaded).

void main(string args) {
    if (args == "" || args == 0) {
        write("Usage: load <path>");
        write("Example: load /std/object");
        return;
    }

    // Check if already loaded
    object existing = find_object(args);

    if (existing) {
        // Use update to reload
        int count = update(args);
        write("Updated " + args + " (" + count + " object(s) affected)");
    } else {
        // Fresh load
        object obj = load_object(args);
        if (obj) {
            write("Loaded: " + object_name(obj));
        } else {
            write("Failed to load: " + args);
        }
    }
}
