// /world/rooms/town/healer.c
// The Healer's Cottage

inherit "/std/room";

void create() {
    ::create();

    set_short("Healer's Cottage");
    set_long(
        "Bundles of dried herbs hang from the rafters of this small cottage, filling " +
        "the air with a complex mixture of medicinal scents. Shelves line the walls, " +
        "crowded with glass bottles, ceramic jars, and bound bundles of various plants. " +
        "A simple cot sits in one corner for treating patients, while a worn wooden " +
        "table serves as a workspace for preparing poultices and potions."
    );

    add_exit("east", "/world/rooms/town/temple_road_north");
}
