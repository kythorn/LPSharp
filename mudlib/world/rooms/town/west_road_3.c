// /world/rooms/town/west_road_3.c
// Western road near the gate

inherit "/std/room";

void create() {
    ::create();

    set_short("West Road");
    set_long(
        "The road nears the western wall. Stables and wagon yards line the " +
        "street here, serving travelers preparing for long journeys. The " +
        "smell of horses and fresh hay is strong."
    );

    add_exit("east", "/world/rooms/town/craftsman_row");
    add_exit("west", "/world/rooms/town/west_gate");
}
