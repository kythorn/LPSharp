// /world/rooms/forest/edge.c
// The edge of Whisperwood Forest - entrance from town

inherit "/std/room";

void create() {
    ::create();

    set_short("Forest Edge");
    set_long(
        "The cobblestone road from town gives way to a worn dirt path that disappears " +
        "into the shadowy embrace of Whisperwood Forest. Ancient oaks and tall pines " +
        "mark the boundary between civilization and wilderness. Birdsong fills the air, " +
        "and shafts of sunlight filter through the canopy ahead. A weathered sign reads: " +
        "'Beware - wildlife may be dangerous to the unprepared.'"
    );

    add_exit("north", "/world/rooms/town/south_gate");
    add_exit("south", "/world/rooms/forest/sunlit_path");
    add_exit("east", "/world/rooms/forest/mossy_clearing");
}
