// goto.c - Teleport to a room
// Usage: goto <room_path>
// Teleports the wizard to the specified room.

void main(string args) {
    if (args == "" || args == 0) {
        write("Usage: goto <room_path>");
        write("Example: goto /world/rooms/town/square");
        return;
    }

    object player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    object room = load_object(args);

    if (!room) {
        write("Failed to load room: " + args);
        return;
    }

    object old_room = environment(player);
    if (old_room) {
        tell_room(old_room, player->query_name() + " disappears in a puff of smoke.\n", player);
    }

    player->move(room);

    tell_room(room, player->query_name() + " appears in a puff of smoke.\n", player);
    write("Teleported to: " + args);

    // Look at the new room
    command("look");
}
