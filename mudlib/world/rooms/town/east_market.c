// /world/rooms/town/east_market.c
// Small market on the east side of town

inherit "/std/room";

void create() {
    ::create();

    set_short("East Market");
    set_long(
        "A smaller market area serves the eastern residential district. Local\n" +
        "craftsmen sell their goods here - pottery, woven baskets, and simple\n" +
        "tools. The pace is slower and friendlier than the bustling main market\n" +
        "to the west."
    );

    add_exit("west", "/world/rooms/town/east_road_2");
    add_exit("east", "/world/rooms/town/east_road_3");
    add_exit("north", "/world/guilds/mages");
}
