// clone.c - Clone an object into your inventory or current room
// Usage: clone <path> [here]
// Creates a clone of the specified object.
// If 'here' is specified, places it in the room instead of inventory.
// Supports relative paths.

void main(string args) {
    object player;
    object obj;
    string path;
    int to_room;
    string *parts;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        write("Usage: clone <path> [here]");
        write("Example: clone /std/object");
        write("Example: clone weapon.c here");
        return;
    }

    // Check for 'here' flag
    parts = explode(args, " ");
    to_room = 0;
    if (sizeof(parts) > 1 && parts[sizeof(parts) - 1] == "here") {
        to_room = 1;
        args = implode(parts[0..sizeof(parts)-2], " ");
    }

    // Resolve relative path
    path = call_other(player, "resolve_path", args);

    obj = clone_object(path);

    if (!obj) {
        write("Failed to clone: " + path);
        return;
    }

    // Move to room or inventory
    if (to_room) {
        object room;
        room = environment(player);
        if (room) {
            call_other(obj, "move", room);
            write("Cloned " + path + " -> " + object_name(obj) + " (in room)");
        } else {
            write("Cloned " + path + " -> " + object_name(obj) + " (no room)");
        }
    } else {
        call_other(obj, "move", player);
        write("Cloned " + path + " -> " + object_name(obj));
    }
}
