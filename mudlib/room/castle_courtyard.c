// /room/castle_courtyard.c
// The castle's inner courtyard

inherit "/std/room";

void create() {
    ::create();

    set_short("Castle Courtyard");
    set_long(
        "The cobblestone courtyard is surrounded by the towering walls of the castle. " +
        "Soldiers drill in one corner while servants hurry about their duties. " +
        "A stable lines the southern wall, and the smell of horses mingles with the " +
        "scent of leather and oil from the nearby armory. The main keep rises to the " +
        "north, its great doors flanked by stone lions."
    );

    add_exit("west", "/room/castle_gate");
    add_exit("north", "/room/castle_hall");
}
