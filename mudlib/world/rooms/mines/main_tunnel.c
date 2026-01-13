// Main Mine Tunnel

inherit "/std/room";

void create() {
    ::create();

    set_short("Main Tunnel");
    set_long(
        "This wider tunnel was once the main thoroughfare of the mine. " +
        "Cart tracks are still visible on the floor, though they're now " +
        "warped and broken. The air is thick and heavy, and a foul smell " +
        "permeates everything - the unmistakable stench of troll. Deep " +
        "growling echoes off the walls from somewhere in the darkness."
    );

    add_exit("up", "/world/rooms/mines/upper_shaft");
    add_exit("west", "/world/rooms/mines/troll_den");
    add_exit("south", "/world/rooms/mines/flooded_shaft");

    add_spawn("/world/mobs/troll");
    enable_reset(120);
}
