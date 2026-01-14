// /world/rooms/town/north_gate.c
// The North Gate of town

inherit "/std/room";

void create() {
    ::create();

    set_short("North Gate");
    set_long(
        "The massive North Gate stands before you, its iron-bound wooden doors\n" +
        "currently open to allow passage. Guards in chainmail keep watch from\n" +
        "towers on either side. Beyond the gate lies the wilderness - rolling\n" +
        "hills and distant mountains. Most travelers prefer the safer southern\n" +
        "road to the forest."
    );

    add_exit("south", "/world/rooms/town/north_road");
    // No exit north - gate leads to wilderness not yet implemented
}
