/*
 * /cmds/admin/update.c - Hot-reload command for admins
 *
 * Usage: update <path>
 *
 * Recompiles an object and all objects that inherit from it.
 * Existing clones keep their old code (conservative strategy).
 * Supports relative paths.
 */

void main(string arg) {
    object player;
    string path;
    int count;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!arg || arg == "") {
        write("Usage: update <path>");
        write("  Example: update /std/object");
        write("  Example: update weapon.c");
        return;
    }

    // Resolve relative path
    path = call_other(player, "resolve_path", arg);

    write("Updating " + path + "...");

    count = update(path);

    if (count > 0) {
        write("Successfully updated " + count + " object(s).");
    } else {
        write("Update failed or no objects found.");
    }
}
