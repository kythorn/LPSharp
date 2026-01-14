// /world/rooms/town/east_road_2.c
// Continuation of East Road

inherit "/std/room";

void create() {
    ::create();

    set_short("East Road");
    set_long(
        "The road continues past a row of modest houses. Children play in " +
        "small yards while their parents tend to daily chores. A water well " +
        "stands at a small intersection, where locals gather to chat and " +
        "draw water."
    );

    add_exit("west", "/world/rooms/town/east_road");
    add_exit("east", "/world/rooms/town/east_market");
}
