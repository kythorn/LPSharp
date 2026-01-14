// /world/rooms/special/netherworld.c
// The realm of the dead - where players go when they die
// Players can use "resurrect" to return to the living world

inherit "/std/room";

void create() {
    ::create();
    set_short("The Netherworld");
    set_long(
        "You float in an endless gray void. Whispers of the dead echo around you,\n" +
        "and a pale mist obscures everything beyond arm's reach. You are not truly\n" +
        "dead, merely... between. Your mortal form lies somewhere in the world of\n" +
        "the living, waiting for your return.\n\n" +
        "Type 'resurrect' to return to the world of the living."
    );

    // No exits - players must resurrect to leave
}

// Override init to add resurrect action and remind players
void init() {
    ::init();
    object player;
    player = this_player();
    if (player) {
        add_action("do_resurrect", "resurrect");
        call_out("remind_player", 5, player);
    }
}

int do_resurrect(string args) {
    object player;
    object town_square;

    player = this_player();
    if (!player) {
        return 0;
    }

    write("You feel a warm light pulling you back to the mortal realm...\n");
    write("Your spirit returns to your body.\n\n");

    // Move to town square
    town_square = load_object("/world/rooms/town/square");
    if (town_square) {
        move_object(player, town_square);
        tell_room(town_square, call_other(player, "query_name") +
                  " has returned from the dead!\n", player);
        command("look");
    }

    // Save player state
    call_other(player, "save_player");

    return 1;
}

void remind_player(object player) {
    if (player && environment(player) == this_object()) {
        tell_object(player, "A voice whispers: \"Type 'resurrect' to return to the living...\"\n");
    }
}
