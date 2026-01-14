// /world/rooms/town/north_road.c
// Road leading to the North Gate

inherit "/std/room";

void create() {
    ::create();

    set_short("North Road");
    set_long(
        "The road narrows as it approaches the northern wall of the town. Guard\n" +
        "towers loom ahead, and you can see the massive North Gate that leads to\n" +
        "the wilderness beyond. Travelers and merchants mill about, preparing for\n" +
        "their journeys."
    );

    add_exit("south", "/world/rooms/town/temple_plaza");
    add_exit("north", "/world/rooms/town/north_gate");
}
