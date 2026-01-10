// /room/castle_hall.c
// The great hall of the castle

inherit "/std/room";

void create() {
    ::create();

    set_short("Castle Great Hall");
    set_long(
        "You stand in the magnificent great hall of the castle. Massive chandeliers " +
        "hang from the vaulted ceiling, their countless candles casting warm light " +
        "across the polished stone floor. Tapestries depicting heroic battles and " +
        "royal hunts adorn the walls. At the far end, upon a raised dais, sits an " +
        "ornate throne of carved oak and gold. Guards stand at silent attention along " +
        "the walls, ever watchful."
    );

    add_exit("south", "/room/castle_courtyard");
}
