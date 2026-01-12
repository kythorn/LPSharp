// /cmds/quit.c
// Quit command - save and disconnect from the game

void main(string args) {
    object player;

    player = this_player();
    if (player != 0) {
        call_other(player, "save_player");
    }

    write("Goodbye!");
    destruct(player);
}
