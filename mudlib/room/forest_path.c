// /room/forest_path.c
// A path leading into the forest

inherit "/std/room";

void create() {
    ::create();

    set_short("Forest Path");
    set_long(
        "The dirt road narrows to a winding path as it enters the forest. Ancient oaks " +
        "and towering pines crowd close on either side, their branches intertwining to " +
        "form a shadowy canopy overhead. Shafts of sunlight pierce through gaps in the " +
        "leaves, dappling the ground with golden spots. The forest is alive with the " +
        "sounds of birds and the rustling of unseen creatures in the undergrowth."
    );

    add_exit("north", "/room/crossroads");
    add_exit("south", "/room/deep_forest");

    // Hidden exit - only those who look carefully might find it
    add_hidden_exit("west", "/room/hidden_grove");
}
