// /cmds/std/go.c
// Movement command - move in a direction

void main(string args) {
    object room;
    object player;
    object destination;
    string exit_path;
    string dir;

    if (args == 0) {
        write("Go where?");
        return;
    }

    dir = args;

    player = this_player();
    if (player == 0) {
        write("You have no physical form to move.");
        return;
    }

    room = environment(player);
    if (room == 0) {
        write("You are in a void. There is nowhere to go.");
        return;
    }

    // Query the exit in the given direction
    exit_path = call_other(room, "query_exit", dir);

    if (exit_path == "") {
        write("You cannot go that way.");
        return;
    }

    // Load the destination room
    destination = load_object(exit_path);
    if (destination == 0) {
        write("That exit leads nowhere.");
        return;
    }

    // Check if this is a hidden exit
    int is_hidden;
    is_hidden = call_other(room, "is_hidden_exit", dir);

    // Notify old room and player
    if (is_hidden) {
        // Hidden exit - others just see player leave, no direction
        call_other(room, "act", player,
            "You go " + dir + ".",
            "$N leaves.");
    } else {
        // Normal exit - show direction
        call_other(room, "act", player,
            "You go " + dir + ".",
            "$N leaves " + dir + ".");
    }

    // Move the player
    move_object(destination);

    // Notify new room that player has arrived (after move)
    call_other(destination, "act", player,
        "",
        "$N arrives.");

    write("");

    // Display the new room using look command
    object look_cmd;
    look_cmd = load_object("/cmds/std/look");
    call_other(look_cmd, "main", "");
}
