// /room/castle_gate.c
// The entrance to the castle grounds

inherit "/std/room";

void create() {
    ::create();

    set_short("Castle Gate");
    set_long(
        "You stand before the imposing gatehouse of the castle. Twin towers of gray " +
        "stone flank a heavy portcullis, currently raised to allow passage. Guards in " +
        "polished armor stand at attention, their halberds gleaming. Beyond the gate, " +
        "a cobbled courtyard leads to the castle's main keep. The town square lies to " +
        "the west, while the road continues east toward the castle proper."
    );

    add_exit("west", "/room/town_square");
    add_exit("east", "/room/castle_courtyard");
}
