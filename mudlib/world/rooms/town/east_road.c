// /world/rooms/town/east_road.c
// East Road leading from Town Square

inherit "/std/room";

void create() {
    ::create();

    set_short("East Road");
    set_long(
        "A well-maintained road leads eastward from the town square. " +
        "Residential buildings and small workshops line both sides. " +
        "The sounds of craftsmen at work drift from open windows, " +
        "and the smell of baking bread wafts from a nearby home."
    );

    add_exit("west", "/world/rooms/town/square");
    add_exit("east", "/world/rooms/town/east_road_2");
}
