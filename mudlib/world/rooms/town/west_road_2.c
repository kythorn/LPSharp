// /world/rooms/town/west_road_2.c
// Continuation of West Road

inherit "/std/room";

void create() {
    ::create();

    set_short("West Road");
    set_long(
        "The road continues past a row of boarding houses and small inns. This\n" +
        "area caters to travelers and merchants who need affordable lodging. A\n" +
        "notice board displays advertisements for rooms and services."
    );

    add_exit("east", "/world/rooms/town/west_road");
    add_exit("west", "/world/rooms/town/craftsman_row");
}
