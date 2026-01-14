// /world/rooms/town/square.c
// The central town square - starting location for new players

inherit "/std/room";

void create() {
    ::create();

    set_short("Town Square");
    set_long(
        "You stand in the heart of the town, a bustling square paved with worn\n" +
        "cobblestones. A weathered stone fountain stands at the center, its water\n" +
        "sparkling in the light. Merchants call out their wares from wooden stalls,\n" +
        "while townspeople hurry about their daily business. Roads lead away in all\n" +
        "four cardinal directions."
    );

    // Roads lead in cardinal directions
    add_exit("north", "/world/rooms/town/temple_road");
    add_exit("south", "/world/rooms/town/market_street");
    add_exit("east", "/world/rooms/town/east_road");
    add_exit("west", "/world/rooms/town/west_road");
}
