// cat.c - Display contents of a file
// Usage: cat <path>
// Displays the contents of a file in the mudlib.
// Supports relative paths.

void main(string args) {
    object player;
    string path;
    string content;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        write("Usage: cat <path>");
        write("Example: cat /std/room.c");
        write("Example: cat room.c");
        return;
    }

    // Resolve relative path
    path = call_other(player, "resolve_path", args);

    // Read the file
    content = read_file(path);

    if (!content) {
        write("Cannot read file: " + path);
        return;
    }

    write(content);
}
