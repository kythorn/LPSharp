// dest.c - Destruct an object
// Usage: dest <target>
// Destroys an object by name in room/inventory, or by object path.

void main(string args) {
    object player;
    object room;
    object obj;
    string name;
    string path;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        write("Usage: dest <target>");
        write("Example: dest sword       (in room or inventory)");
        write("Example: dest /std/object#1234");
        return;
    }

    // First try to find by name in room
    room = environment(player);
    if (room) {
        obj = present(args, room);
    }

    // Then try inventory
    if (!obj) {
        obj = present(args, player);
    }

    // Then try as object path (with path resolution)
    if (!obj) {
        path = call_other(player, "resolve_path", args);
        obj = find_object(path);
    }

    // Also try the raw argument as object path
    if (!obj) {
        obj = find_object(args);
    }

    if (!obj) {
        write("Object not found: " + args);
        return;
    }

    name = object_name(obj);
    destruct(obj);
    write("Destructed: " + name);
}
