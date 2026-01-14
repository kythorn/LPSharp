// /world/rooms/town/south_road.c
// Road leading to South Gate

inherit "/std/room";

void create() {
    ::create();

    set_short("South Road");
    set_long(
        "The road widens here as it approaches the South Gate. Guard barracks and\n" +
        "storehouses line the sides, ready to supply the town's defenses. Travelers\n" +
        "prepare for their journeys, checking supplies and adjusting their gear\n" +
        "before heading out into the wilderness."
    );

    add_exit("north", "/world/rooms/town/lower_market");
    add_exit("south", "/world/rooms/town/south_gate");
}
