// /cmds/std/leave.c
// Leave a guild

int main(string args) {
    object player;
    object room;

    player = this_player();
    if (!player) {
        return 0;
    }

    room = environment(player);
    if (!room) {
        write("You are nowhere.\n");
        return 1;
    }

    // Check if the room is a guild
    if (!call_other(room, "query_guild_name")) {
        write("This is not a guild. Go to a guild hall to leave.\n");
        return 1;
    }

    // Try to leave
    call_other(room, "do_leave", player);
    return 1;
}
