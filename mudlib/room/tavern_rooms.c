// /room/tavern_rooms.c
// Upstairs hallway in the tavern

inherit "/std/room";

void create() {
    ::create();

    set_short("Tavern Hallway");
    set_long(
        "A narrow hallway runs the length of the tavern's upper floor, lit by a few " +
        "guttering candles in wall sconces. Several wooden doors line both sides, " +
        "leading to modest rooms available for rent. The floorboards creak beneath " +
        "your feet, and the muffled sounds of the common room drift up from below."
    );

    add_exit("down", "/room/tavern");
}
