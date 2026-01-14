// /world/rooms/town/east_gate.c
// The East Gate of town

inherit "/std/room";

void create() {
    ::create();

    set_short("East Gate");
    set_long(
        "The East Gate is smaller and less busy than its southern counterpart. A\n" +
        "single guard watches the entrance, which leads to farmlands and small\n" +
        "villages. Farmers occasionally bring their produce through this quieter\n" +
        "entrance."
    );

    add_exit("west", "/world/rooms/town/east_road_3");
    // No exit east - leads to farmlands not yet implemented
}
