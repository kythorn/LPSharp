// Upper Mine Shaft - Tier 5 area begins
// Connects from Goblin Caves

inherit "/std/room";

void create() {
    ::create();

    set_short("Upper Mine Shaft");
    set_long(
        "The natural caves give way to rough-hewn mine tunnels, the walls " +
        "showing the marks of ancient picks. Abandoned mining equipment " +
        "lies scattered about - rusted carts, broken tools, and rotting " +
        "support beams. Whatever was mined here long ago has attracted " +
        "darker inhabitants. Webs stretch across corners, and the sound " +
        "of heavy footsteps echoes from below."
    );

    add_exit("up", "/world/rooms/caves/deep_caves");
    add_exit("down", "/world/rooms/mines/main_tunnel");
    add_exit("east", "/world/rooms/mines/spider_nest");

    enable_reset(120);
    add_spawn("/world/mobs/cave_spider");
}
