// Goblin Cave Tunnel

inherit "/std/room";

void create() {
    ::create();

    set_short("Winding Tunnel");
    set_long(
        "The tunnel winds deeper into the earth, its walls slick with " +
        "moisture and covered in crude goblin drawings - stick figures " +
        "fighting, strange symbols, and what might be a map. The passage " +
        "is cramped, forcing you to duck in places. The sounds of goblin " +
        "activity grow louder ahead."
    );

    add_exit("west", "/world/rooms/caves/entrance");
    add_exit("north", "/world/rooms/caves/guard_post");
    add_exit("east", "/world/rooms/caves/warren");

    enable_reset(90);
    add_spawn("/world/mobs/goblin");
}
