// /room/blacksmith.c
// The Ironforge Smithy

inherit "/std/room";

void create() {
    ::create();

    set_short("The Ironforge Smithy");
    set_long(
        "The heat from the forge hits you like a wall as you enter this smoky workshop. " +
        "A massive stone forge dominates the center of the room, its coals glowing " +
        "cherry-red. Hammers, tongs, and other tools hang from hooks on the walls. " +
        "Racks display finished weapons and armor - swords, shields, helms, and " +
        "chainmail glinting in the firelight. The rhythmic clang of hammer on anvil " +
        "echoes through the space."
    );

    add_exit("east", "/room/market_street");
}
