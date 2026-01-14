// /world/rooms/town/general_store.c
// Grimwald's General Store

inherit "/std/room";

void create() {
    ::create();

    set_short("Grimwald's General Store");
    set_long(
        "This cluttered shop seems to stock a bit of everything. Barrels of grain\n" +
        "and dried goods line one wall, while the other displays tools, rope,\n" +
        "lanterns, and other adventuring supplies. A glass case near the counter\n" +
        "contains smaller valuables - tinderboxes, compasses, and vials of\n" +
        "mysterious liquids. The elderly shopkeeper watches you with keen eyes\n" +
        "from behind the counter."
    );

    add_exit("west", "/world/rooms/town/market_street");
}
