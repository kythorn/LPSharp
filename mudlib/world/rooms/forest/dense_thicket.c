// /world/rooms/forest/dense_thicket.c
// Overgrown area - snakes

inherit "/std/room";

void create() {
    ::create();

    set_short("Dense Thicket");
    set_long(
        "Thorny brambles and tangled vines make passage difficult here. The vegetation " +
        "is so thick that you can barely see more than a few feet ahead. Something " +
        "slithers through the undergrowth nearby. Watch your step - this looks like " +
        "the perfect habitat for snakes."
    );

    add_exit("north", "/world/rooms/forest/mushroom_grove");
    add_exit("east", "/world/rooms/forest/stream_crossing");

    // Spawn snakes here
    add_spawn("/world/mobs/snake");
    add_spawn("/world/mobs/snake");
    enable_reset(120);
}
