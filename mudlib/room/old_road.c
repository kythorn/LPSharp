// /room/old_road.c
// An old, overgrown road

inherit "/std/room";

void create() {
    ::create();

    set_short("Old Road");
    set_long(
        "This road was once well-traveled, but now lies mostly forgotten. Weeds push " +
        "through cracks in the old paving stones, and grass encroaches from the sides. " +
        "The skeletal remains of a waystation stand crumbling beside the path. " +
        "Something about this place feels melancholy, as if it remembers better days " +
        "when merchants and travelers passed this way."
    );

    add_exit("east", "/room/crossroads");
    add_exit("west", "/room/ruins");
}
