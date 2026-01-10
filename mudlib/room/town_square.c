// /room/town_square.c
// The central town square - starting location for new players

inherit "/std/room";

void create() {
    ::create();

    set_short("Town Square");
    set_long(
        "You stand in the heart of the town, a bustling square paved with worn cobblestones. " +
        "A weathered stone fountain stands at the center, its water sparkling in the light. " +
        "Merchants call out their wares from wooden stalls, while townspeople hurry about " +
        "their daily business. The imposing towers of the castle loom to the east, while " +
        "the cheerful sounds of a tavern drift from the west."
    );

    // Main exits
    add_exit("north", "/room/temple_road");
    add_exit("south", "/room/market_street");
    add_exit("east", "/room/castle_gate");
    add_exit("west", "/room/tavern");
}
