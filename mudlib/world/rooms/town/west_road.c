// /world/rooms/town/west_road.c
// West Road leading from Town Square

inherit "/std/room";

void create() {
    ::create();

    set_short("West Road");
    set_long(
        "A lively road leads westward from the town square. Street performers\n" +
        "occasionally entertain passersby here, and the smell of roasting meat\n" +
        "from nearby eateries fills the air."
    );

    add_exit("east", "/world/rooms/town/square");
    add_exit("west", "/world/rooms/town/west_road_2");
}
