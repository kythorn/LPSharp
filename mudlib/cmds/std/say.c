// /cmds/say.c
// Say command - broadcasts a message

void main(string args) {
    if (args == "" || args == 0) {
        write("Say what?");
        return;
    }

    write("You say: " + args);
    // TODO Milestone 8: tell_room(environment(this_player()), ...)
}
