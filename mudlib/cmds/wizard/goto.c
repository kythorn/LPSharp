// goto.c - Teleport to a room
// Usage: goto <room_path>
// Teleports the wizard to the specified room.
// Supports relative paths.

void main(string args) {
    object player;
    object room;
    object old_room;
    string path;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    if (!args || args == "") {
        write("Usage: goto <room_path>");
        write("Example: goto /world/rooms/town/square");
        write("Example: goto ../forest/edge");
        return;
    }

    // Resolve relative path
    path = call_other(player, "resolve_path", args);

    room = load_object(path);

    if (!room) {
        write("Failed to load room: " + path);
        return;
    }

    old_room = environment(player);
    if (old_room) {
        tell_room(old_room, call_other(player, "query_name") + " disappears in a puff of smoke.\n", player);
    }

    call_other(player, "move", room);

    tell_room(room, call_other(player, "query_name") + " appears in a puff of smoke.\n", player);
    write("Teleported to: " + path);

    // Look at the new room
    command("look");
}
