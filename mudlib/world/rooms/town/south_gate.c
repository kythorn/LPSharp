// /world/rooms/town/south_gate.c
// The southern gate of town

inherit "/std/room";

void create() {
    ::create();

    set_short("South Gate");
    set_long(
        "The massive wooden gates of the town's southern entrance stand open\n" +
        "during daylight hours, flanked by stone towers where guards keep watch.\n" +
        "A well-worn dirt road leads south through the gates toward the farmlands\n" +
        "and forests beyond. Wagons loaded with goods trundle in and out, while\n" +
        "guards inspect travelers and collect tolls from merchants."
    );

    add_exit("north", "/world/rooms/town/south_road");
    add_exit("south", "/world/rooms/forest/edge");
}
