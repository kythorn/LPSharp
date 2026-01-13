// Dragon's Treasure Room

inherit "/std/room";

void create() {
    ::create();

    set_short("Treasure Chamber");
    set_long(
        "Mountains of gold coins, gemstones, and precious artifacts fill " +
        "this chamber - the dragon's hoard accumulated over centuries. " +
        "Ancient weapons and armor, magical items, and treasures from " +
        "fallen kingdoms glitter in the firelight. Yet the dragon guards " +
        "its wealth jealously - none may take from this hoard while the " +
        "beast still lives."
    );

    add_exit("west", "/world/rooms/dragon/fire_cavern");

    enable_reset(300);
    add_spawn("/world/mobs/fire_elemental");
}
