// /world/rooms/wilderness/deep_forest.c
// Deep within the forest

inherit "/std/room";

void create() {
    ::create();

    set_short("Deep Forest");
    set_long(
        "The forest grows darker and more primeval here. Massive trees, some wider than " +
        "a wagon, rise like pillars toward the barely-visible sky. Thick moss carpets " +
        "the ground and climbs the ancient trunks. The air is cool and damp, filled " +
        "with the earthy smell of decay and new growth. Strange sounds echo in the " +
        "distance - this is wild country, best traveled with caution."
    );

    add_exit("north", "/world/rooms/wilderness/forest_path");
}
