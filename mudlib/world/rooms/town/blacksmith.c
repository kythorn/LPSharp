// /world/rooms/town/blacksmith.c
// The Ironforge Smithy

inherit "/std/room";

void create() {
    ::create();

    set_short("The Ironforge Smithy");
    set_long(
        "The heat from the forge hits you like a wall as you enter this smoky\n" +
        "workshop. A massive stone forge dominates the center of the room, its\n" +
        "coals glowing cherry-red. Hammers, tongs, and other tools hang from hooks\n" +
        "on the walls. Racks display finished weapons and armor - swords, shields,\n" +
        "helms, and chainmail glinting in the firelight. The rhythmic clang of\n" +
        "hammer on anvil echoes through the space."
    );

    add_exit("east", "/world/rooms/town/market_street");
}
