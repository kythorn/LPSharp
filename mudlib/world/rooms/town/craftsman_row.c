// /world/rooms/town/craftsman_row.c
// Craftsman Row - artisan district

inherit "/std/room";

void create() {
    ::create();

    set_short("Craftsman Row");
    set_long(
        "This section of town is home to various artisans and craftsmen. The\n" +
        "rhythmic sound of hammers on anvils and the whir of potter's wheels\n" +
        "create a constant backdrop of industry. Workshops display finished\n" +
        "goods in their windows."
    );

    add_exit("east", "/world/rooms/town/west_road_2");
    add_exit("west", "/world/rooms/town/west_road_3");
}
