// /cmds/quit.c
// Quit command - save and disconnect from the game

void main(string args) {
    object player;
    string name;

    player = this_player();
    if (player != 0) {
        name = call_other(player, "query_name");
        call_other(player, "save_player");
        log_console("player", name + " logged out");
    }

    write("Goodbye!");
    destruct(player);
}
