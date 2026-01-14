// /world/rooms/town/square.c
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
    add_exit("north", "/world/rooms/town/temple_road");
    add_exit("south", "/world/rooms/town/market_street");
    add_exit("east", "/world/rooms/castle/gate");
    add_exit("west", "/world/rooms/town/tavern");

    // Guild halls
    add_exit("northeast", "/world/guilds/fighters");
    add_exit("northwest", "/world/guilds/mages");
    add_exit("southeast", "/world/guilds/healers");
}
