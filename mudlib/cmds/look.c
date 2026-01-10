// /cmds/look.c
// Look command - examine surroundings or objects

void main(string args) {
    object room;
    object player;
    string short_desc;
    string long_desc;
    string exits;

    player = this_player();
    if (player == 0) {
        write("You have no physical form.");
        return;
    }

    room = environment(player);
    if (room == 0) {
        write("You are floating in a void.");
        write("There is nothing here.");
        return;
    }

    // Get room descriptions using call_other
    short_desc = call_other(room, "query_short");
    long_desc = call_other(room, "query_long");
    exits = call_other(room, "query_exits");

    // Display the room
    write(short_desc);
    write(long_desc);
    write("");
    write(exits);

    // TODO: Show other players and objects in room
}
