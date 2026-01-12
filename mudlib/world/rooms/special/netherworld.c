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

// Override init to remind players they can resurrect
void init() {
    ::init();
    object player;
    player = this_player();
    if (player) {
        call_out("remind_player", 5, player);
    }
}

void remind_player(object player) {
    if (player && environment(player) == this_object()) {
        tell_object(player, "A voice whispers: \"Type 'resurrect' to return to the living...\"\n");
    }
}
