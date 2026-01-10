// /room/farm_road.c
// A road through farmland

inherit "/std/room";

void create() {
    ::create();

    set_short("Farm Road");
    set_long(
        "A dusty road winds through rolling farmland. Golden fields of wheat sway in " +
        "the breeze on one side, while neat rows of vegetables stretch across the other. " +
        "In the distance, you can see farmhouses with thatched roofs and wisps of " +
        "smoke rising from their chimneys. The lowing of cattle and bleating of sheep " +
        "drift across the pastoral landscape."
    );

    add_exit("west", "/room/crossroads");
}
