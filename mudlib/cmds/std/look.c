// /cmds/std/look.c
// Look command - examine surroundings or objects

void main(string args) {
    object room;
    object player;
    string short_desc;
    string long_desc;
    string exits;
    object *contents;
    int i;

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

    // Get room descriptions using call_other
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
                name = call_other(contents[i], "query_short");
                if (!name || name == "") {
                    name = call_other(contents[i], "query_name");
                }
                if (name && name != "") {
                    write(capitalize(name) + " is here.");
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
