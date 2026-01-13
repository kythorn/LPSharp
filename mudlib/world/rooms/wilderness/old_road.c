// /world/rooms/wilderness/old_road.c
// An old, overgrown road

inherit "/std/room";

void create() {
    ::create();

    set_short("Old Road");
    set_long(
        "This road was once well-traveled, but now lies mostly forgotten. Weeds push " +
        "through cracks in the old paving stones, and grass encroaches from the sides. " +
        "The skeletal remains of a waystation stand crumbling beside the path. " +
        "To the north, a dark cave entrance gapes in the hillside, the stench of " +
        "goblins wafting from within. Something about this place feels dangerous."
    );

    add_exit("east", "/world/rooms/wilderness/crossroads");
    add_exit("west", "/world/rooms/wilderness/ruins/entrance");
    add_exit("north", "/world/rooms/caves/entrance");
}
