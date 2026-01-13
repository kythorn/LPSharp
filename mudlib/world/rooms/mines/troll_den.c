// Troll Den

inherit "/std/room";

void create() {
    ::create();

    set_short("Troll Den");
    set_long(
        "This large chamber has become home to cave trolls. Piles of bones " +
        "from their meals litter the floor, and the smell is overpowering. " +
        "Crude bedding made from stolen materials fills one corner. The " +
        "trolls have accumulated a hoard of items taken from those foolish " +
        "enough to venture this deep - mostly broken, but some might be " +
        "salvageable."
    );

    add_exit("east", "/world/rooms/mines/main_tunnel");

    enable_reset(180);
    add_spawn("/world/mobs/troll");
    add_spawn("/world/mobs/troll");
}
