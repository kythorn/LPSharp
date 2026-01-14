// /world/rooms/forest/mossy_clearing.c
// A clearing with soft moss - easy monsters

inherit "/std/room";

void create() {
    ::create();

    set_short("Mossy Clearing");
    set_long(
        "A peaceful clearing opens up among the trees, carpeted with soft green moss. " +
        "A fallen log provides a natural resting spot, and acorns litter the ground " +
        "beneath a massive oak tree. Squirrels chatter in the branches above, " +
        "occasionally darting down to gather food."
    );

    add_exit("west", "/world/rooms/forest/edge");
    add_exit("south", "/world/rooms/forest/old_oak");

    // Spawn a squirrel here
    add_spawn("/world/mobs/squirrel");
    enable_reset(120);
}
