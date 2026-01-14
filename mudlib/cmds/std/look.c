// /cmds/std/look.c
// Look command - examine surroundings or objects

// Look at a specific object
void look_at_object(object target) {
    string short_desc;
    string long_desc;
    object *contents;
    int i;

    short_desc = call_other(target, "query_short");
    long_desc = call_other(target, "query_long");

    // Show object description
    if (short_desc && short_desc != "") {
        write(capitalize(short_desc) + ".");
    }
    if (long_desc && long_desc != "") {
        write(long_desc);
    } else if (!short_desc || short_desc == "") {
        write("You see nothing special.");
    }

    // Show health status for living things
    if (call_other(target, "is_living")) {
        string health;
        health = call_other(target, "query_health_desc");
        if (health && health != "") {
            write(capitalize(call_other(target, "query_name")) + " is " + health + ".");
        }
    }

    // Show contents if object has inventory (corpses, containers, etc.)
    contents = all_inventory(target);
    if (sizeof(contents) > 0) {
        write("");
        write("It contains:");
        for (i = 0; i < sizeof(contents); i++) {
            string item_desc;
            item_desc = call_other(contents[i], "query_short");
            if (item_desc && item_desc != "") {
                write("  " + capitalize(item_desc));
            }
        }
    }
}

// Look at the current room
void look_at_room() {
    object player;
    object room;
    string short_desc;
    string long_desc;
    string exits;
    object *contents;
    int i;

    player = this_player();
    room = environment(player);

    // Get room descriptions
    short_desc = call_other(room, "query_short");
    long_desc = call_other(room, "query_long");
    exits = call_other(room, "query_exits");

    // Display the room
    write(short_desc);
    write(long_desc);
    write("");
    write(exits);

    // Show other players and livings in room
    contents = all_inventory(room);
    for (i = 0; i < sizeof(contents); i++) {
        if (contents[i] != player) {
            if (call_other(contents[i], "is_living")) {
                string name;
                string health;
                name = call_other(contents[i], "query_short");
                if (!name || name == "") {
                    name = call_other(contents[i], "query_name");
                }
                health = call_other(contents[i], "query_health_desc");
                if (name && name != "") {
                    if (health && health != "in perfect health") {
                        write(capitalize(name) + " is here, " + health + ".");
                    } else {
                        write(capitalize(name) + " is here.");
                    }
                }
            }
        }
    }

    // Show items on the ground
    for (i = 0; i < sizeof(contents); i++) {
        if (contents[i] != player && !call_other(contents[i], "is_living")) {
            string item_desc;
            item_desc = call_other(contents[i], "query_short");
            if (item_desc && item_desc != "") {
                write(capitalize(item_desc) + " is lying here.");
            }
        }
    }
}

void main(string args) {
    object player;
    object room;
    object target;

    player = this_player();
    if (player == 0) {
        write("You have no physical form.");
        return;
    }

    room = environment(player);
    if (room == 0) {
        write("You are floating in a void.");
        write("There is nothing here.");
        return;
    }

    // No args - look at the room
    if (!args || args == "") {
        look_at_room();
        return;
    }

    // Handle "look at <object>" or "look <object>"
    if (sscanf(args, "at %s", args) != 1) {
        // Just "look <object>" - args is unchanged
    }

    // Try to find the object in the room or player's inventory
    target = present(args, room);
    if (!target) {
        target = present(args, player);
    }

    if (!target) {
        write("You don't see that here.");
        return;
    }

    look_at_object(target);
}
