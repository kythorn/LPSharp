// /cmds/say.c
// Say command - broadcasts a message to everyone in the room

void main(string args) {
    object player;
    object room;

    if (args == "" || args == 0) {
        write("Say what?\n");
        return;
    }

    player = this_player();
    room = environment(player);

    if (!room) {
        write("You are nowhere.\n");
        return;
    }

    // Use the act() system for consistent messaging
    // Note: act() adds newlines automatically
    call_other(room, "act", player,
        "You say: " + args,
        "$N says: " + args);
}
