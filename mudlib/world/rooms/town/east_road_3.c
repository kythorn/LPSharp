// /world/rooms/town/east_road_3.c
// Eastern road near the gate

inherit "/std/room";

void create() {
    ::create();

    set_short("East Road");
    set_long(
        "The road approaches the eastern wall of town. The buildings here " +
        "are older and more weathered. A few warehouses store goods for " +
        "merchants trading with villages to the east."
    );

    add_exit("west", "/world/rooms/town/east_market");
    add_exit("east", "/world/rooms/town/east_gate");
}
