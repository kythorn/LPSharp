// /world/rooms/town/tavern_rooms.c
// Upstairs hallway in the tavern

inherit "/std/room";

void create() {
    ::create();

    set_short("Tavern Hallway");
    set_long(
        "A narrow hallway runs the length of the tavern's upper floor, lit by a\n" +
        "few guttering candles in wall sconces. Several wooden doors line both\n" +
        "sides, leading to modest rooms available for rent. The floorboards creak\n" +
        "beneath your feet, and the muffled sounds of the common room drift up\n" +
        "from below."
    );

    add_exit("down", "/world/rooms/town/tavern");
}
