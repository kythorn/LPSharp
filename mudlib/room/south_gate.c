// /room/south_gate.c
// The southern gate of town

inherit "/std/room";

void create() {
    ::create();

    set_short("South Gate");
    set_long(
        "The massive wooden gates of the town's southern entrance stand open during " +
        "daylight hours, flanked by stone towers where guards keep watch. A well-worn " +
        "dirt road leads south through the gates toward the farmlands and forests " +
        "beyond. Wagons loaded with goods trundle in and out, while guards inspect " +
        "travelers and collect tolls from merchants."
    );

    add_exit("north", "/room/market_street");
    add_exit("south", "/room/crossroads");
}
