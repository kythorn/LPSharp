// /cmds/std/who.c
// Who command - list players currently in the game

void main(string args) {
    object *active;
    object *linkdead;
    object player;
    string name;
    int i;
    int total;

    active = users();
    linkdead = linkdead_users();
    total = sizeof(active) + sizeof(linkdead);

    write("Players online: " + total);
    write("----------------------------------------");

    // Show active players
    for (i = 0; i < sizeof(active); i++) {
        player = active[i];
        if (player != 0) {
            name = call_other(player, "query_name");
            if (name == 0 || name == "") {
                name = "Unknown";
            }
            write("  " + name);
        }
    }

    // Show linkdead players
    for (i = 0; i < sizeof(linkdead); i++) {
        player = linkdead[i];
        if (player != 0) {
            name = call_other(player, "query_name");
            if (name == 0 || name == "") {
                name = "Unknown";
            }
            write("  " + name + " (linkdead)");
        }
    }

    write("----------------------------------------");
}
