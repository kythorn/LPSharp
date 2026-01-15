// /cmds/std/bury.c
// Bury a corpse in the current room

void main(string args) {
    object player;
    object room;
    object *contents;
    object corpse;
    object *corpse_contents;
    int i;

    player = this_player();
    if (!player) {
        return;
    }

    room = environment(player);
    if (!room) {
        write("You can't bury anything here.\n");
        return;
    }

    // Find corpses in the room
    contents = all_inventory(room);
    corpse = 0;

    for (i = 0; i < sizeof(contents); i++) {
        // Check if it's a real corpse (has query_corpse_name function)
        if (call_other(contents[i], "query_corpse_name")) {
            // If no args, use first corpse found
            if (!args || args == "") {
                corpse = contents[i];
                break;
            }
            // If args given, check if this corpse matches
            if (call_other(contents[i], "id", args)) {
                corpse = contents[i];
                break;
            }
        }
    }

    if (!corpse) {
        if (!args || args == "") {
            write("There is no corpse here to bury.\n");
        } else {
            write("You don't see that corpse here.\n");
        }
        return;
    }

    // Destroy all contents of the corpse - they're buried with it
    corpse_contents = all_inventory(corpse);
    for (i = 0; i < sizeof(corpse_contents); i++) {
        destruct(corpse_contents[i]);
    }

    // Announce the burial
    string corpse_name;
    corpse_name = call_other(corpse, "query_corpse_name");

    tell_room(room, call_other(player, "query_name") + " buries the corpse of " +
              corpse_name + ".\n", player);
    write("You dig a shallow grave and bury the remains of " + corpse_name + ".\n");

    destruct(corpse);
}
