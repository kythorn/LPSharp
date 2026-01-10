// /room/temple_road.c
// The road leading to the temple district

inherit "/std/room";

void create() {
    ::create();

    set_short("Temple Road");
    set_long(
        "A wide cobblestone road leads northward toward the temple district. " +
        "The stones here are cleaner and better maintained than in the market areas. " +
        "White-robed acolytes occasionally pass by on their way to morning prayers. " +
        "To the south, the bustling sounds of the town square can be heard."
    );

    add_exit("south", "/room/town_square");
    add_exit("north", "/room/temple");
    add_exit("west", "/room/healer");
}
