// ls.c - List directory contents
// Usage: ls [path]
// If no path given, lists current working directory or wizard home.

void main(string args) {
    string path;

    if (args == "" || args == 0) {
        // Default to wizard home directory
        path = homedir();
        if (path == 0) {
            path = "/";
        }
    } else {
        path = args;
    }

    mixed *entries = get_dir(path);

    if (sizeof(entries) == 0) {
        write("No files found or directory does not exist: " + path);
        return;
    }

    write("Contents of " + path + ":");

    int i;
    for (i = 0; i < sizeof(entries); i++) {
        write("  " + entries[i]);
    }

    write(sizeof(entries) + " item(s)");
}
