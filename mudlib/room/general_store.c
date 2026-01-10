// /room/general_store.c
// Grimwald's General Store

inherit "/std/room";

void create() {
    ::create();

    set_short("Grimwald's General Store");
    set_long(
        "This cluttered shop seems to stock a bit of everything. Barrels of grain and " +
        "dried goods line one wall, while the other displays tools, rope, lanterns, " +
        "and other adventuring supplies. A glass case near the counter contains smaller " +
        "valuables - tinderboxes, compasses, and vials of mysterious liquids. " +
        "The elderly shopkeeper watches you with keen eyes from behind the counter."
    );

    add_exit("west", "/room/market_street");
}
