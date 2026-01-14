// /world/rooms/town/market_street_south.c
// Southern section of Market Street

inherit "/std/room";

void create() {
    ::create();

    set_short("Market Street South");
    set_long(
        "The market continues southward, though the stalls here are less crowded.\n" +
        "Second-hand goods and cheaper wares are sold here by vendors who can't\n" +
        "afford the prime spots further north. A few taverns and inns cater to\n" +
        "travelers heading to or from the South Gate."
    );

    add_exit("north", "/world/rooms/town/market_street");
    add_exit("south", "/world/rooms/town/lower_market");
    add_exit("east", "/world/guilds/fighters");
}
