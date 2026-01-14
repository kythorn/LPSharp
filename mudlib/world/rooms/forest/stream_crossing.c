// /world/rooms/forest/stream_crossing.c
// Peaceful rest area - no monsters

inherit "/std/room";

void create() {
    ::create();

    set_short("Stream Crossing");
    set_long(
        "A crystal-clear stream babbles through the forest here, flowing over\n" +
        "smooth stones worn by centuries of water. Stepping stones provide a way\n" +
        "across. The peaceful sound of running water is soothing, and this seems\n" +
        "like a safe place to rest and recover. Small fish dart through the\n" +
        "shallows."
    );

    add_exit("east", "/world/rooms/forest/winding_trail");
    add_exit("west", "/world/rooms/forest/dense_thicket");
    add_exit("southeast", "/world/rooms/forest/ancient_grove");

    // No monsters here - peaceful rest spot
}
