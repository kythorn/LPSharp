// /world/rooms/town/market_street.c
// The busy market street south of town square

inherit "/std/room";

void create() {
    ::create();

    set_short("Market Street");
    set_long(
        "The air is alive with the cries of merchants hawking their wares along\n" +
        "this crowded street. Wooden stalls and canvas-covered carts line both\n" +
        "sides, displaying everything from fresh produce to bolts of colorful\n" +
        "cloth. The smell of spices, leather, and fresh-baked goods mingles\n" +
        "together. Shoppers haggle loudly while street urchins weave between the\n" +
        "crowds."
    );

    add_exit("north", "/world/rooms/town/square");
    add_exit("south", "/world/rooms/town/market_street_south");
    add_exit("east", "/world/rooms/town/general_store");
    add_exit("west", "/world/rooms/town/blacksmith");
}
