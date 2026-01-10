// /world/rooms/wilderness/crossroads.c
// A crossroads outside town

inherit "/std/room";

void create() {
    ::create();

    set_short("Crossroads");
    set_long(
        "You stand at a dusty crossroads where several paths meet. A weathered wooden " +
        "signpost points in various directions, though some of the signs have faded " +
        "beyond reading. The town walls rise to the north, while open countryside " +
        "stretches in all other directions. A few scraggly trees offer meager shade, " +
        "and the distant call of crows echoes across the fields."
    );

    add_exit("north", "/world/rooms/town/south_gate");
    add_exit("south", "/world/rooms/wilderness/forest_path");
    add_exit("east", "/world/rooms/wilderness/farm_road");
    add_exit("west", "/world/rooms/wilderness/old_road");
}
