// Dragon's Lair - Boss Room

inherit "/std/room";

void create() {
    ::create();

    set_short("Dragon's Lair");
    set_long(
        "You have entered the lair of an ancient fire dragon. The massive " +
        "creature coils atop a bed of gold and bones, its scales glinting " +
        "like rubies in the firelight. Smoke curls from its nostrils, and " +
        "its eyes - ancient and terrible - fix upon you with predatory " +
        "intelligence. The heat radiating from its body is intense, and " +
        "you can feel the power of a creature that has lived for millennia. " +
        "This is a battle that will be spoken of in legends... if you survive."
    );

    add_exit("north", "/world/rooms/dragon/fire_cavern");

    add_spawn("/world/mobs/dragon");
    enable_reset(600);  // 10 minute respawn for boss
}

void init() {
    ::init();

    if (this_player()) {
        tell_object(this_player(),
            "\nThe dragon raises its head and fixes its burning gaze upon you.\n" +
            "A deep rumble emanates from its throat - half growl, half laugh.\n" +
            "\"Another mortal comes to test their strength? So be it.\"\n\n");
    }
}
