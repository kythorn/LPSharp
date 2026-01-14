// /world/rooms/forest/winding_trail.c
// Central hub connecting deeper forest areas

inherit "/std/room";

void create() {
    ::create();

    set_short("Winding Trail");
    set_long(
        "The forest path twists and turns here, branching off in several\n" +
        "directions. The trees grow closer together, their branches intertwining\n" +
        "overhead to form a natural tunnel. Animal tracks crisscross the dirt\n" +
        "path - some small, some worryingly large. The forest grows darker to\n" +
        "the south."
    );

    add_exit("north", "/world/rooms/forest/sunlit_path");
    add_exit("south", "/world/rooms/forest/dark_hollow");
    add_exit("east", "/world/rooms/forest/old_oak");
    add_exit("west", "/world/rooms/forest/stream_crossing");

    // Spawn a beetle here
    add_spawn("/world/mobs/beetle");
    enable_reset(120);
}
