// Goblin Warren - Living quarters

inherit "/std/room";

void create() {
    ::create();

    set_short("Goblin Warren");
    set_long(
        "The tunnel opens into a sprawling warren of small chambers and " +
        "alcoves where goblins make their nests. Piles of filthy bedding, " +
        "stolen goods, and half-eaten food fill the spaces. The smell is " +
        "almost unbearable. Passages branch off in several directions, and " +
        "you can hear goblins scurrying about in the darkness."
    );

    add_exit("west", "/world/rooms/caves/tunnel");
    add_exit("down", "/world/rooms/caves/deep_caves");

    enable_reset(90);
    add_spawn("/world/mobs/goblin");
    add_spawn("/world/mobs/goblin");
}
