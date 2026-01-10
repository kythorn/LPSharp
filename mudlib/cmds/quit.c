// /cmds/quit.c
// Quit command - disconnect from the game

void main(string args) {
    write("Goodbye!");
    destruct(this_player());
}
