// /world/rooms/town/temple_plaza.c
// Plaza before the Temple

inherit "/std/room";

void create() {
    ::create();

    set_short("Temple Plaza");
    set_long(
        "A beautiful plaza opens up before the grand Temple of Light. " +
        "White marble benches line the edges where pilgrims rest and pray. " +
        "A large statue of a robed figure with arms raised stands in the center. " +
        "The temple's magnificent facade rises to the west."
    );

    add_exit("south", "/world/rooms/town/temple_road_north");
    add_exit("north", "/world/rooms/town/north_road");
    add_exit("west", "/world/rooms/town/temple");
}
