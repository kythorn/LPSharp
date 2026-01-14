// /world/rooms/town/lower_market.c
// Lower market area near South Gate

inherit "/std/room";

void create() {
    ::create();

    set_short("Lower Market");
    set_long(
        "This area serves as a gathering place for farmers and traders coming " +
        "through the South Gate. Hay wagons and livestock pens line the edges " +
        "of the road. The smell of animals mixes with the scent of fresh produce " +
        "from the surrounding farms."
    );

    add_exit("north", "/world/rooms/town/market_street_south");
    add_exit("south", "/world/rooms/town/south_road");
}
