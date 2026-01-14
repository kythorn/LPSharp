// /world/rooms/forest/sunlit_path.c
// A pleasant path through the forest - easy monsters

inherit "/std/room";

void create() {
    ::create();

    set_short("Sunlit Path");
    set_long(
        "Dappled sunlight warms this gentle forest path. Wildflowers grow along the " +
        "edges, attracting butterflies and bees. The underbrush rustles occasionally " +
        "as small creatures go about their business. This seems like a safe area for " +
        "those new to adventuring."
    );

    add_exit("north", "/world/rooms/forest/edge");
    add_exit("south", "/world/rooms/forest/winding_trail");
    add_exit("west", "/world/rooms/forest/mushroom_grove");

    // Spawn a rabbit here
    add_spawn("/world/mobs/rabbit");
    enable_reset(120);
}
