// /cmds/go.c
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

    // Move the player
    move_object(destination);

    // Show the new room
    write("You go " + dir + ".");
    write("");

    // Display the new room using look command
    object look_cmd;
    look_cmd = load_object("/cmds/look");
    call_other(look_cmd, "main", "");
}
