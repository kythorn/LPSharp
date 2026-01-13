// Fire Cavern - Guards to the dragon

inherit "/std/room";

void create() {
    ::create();

    set_short("Fire Cavern");
    set_long(
        "This vast cavern is filled with pools of molten rock that cast " +
        "everything in hellish red light. Pillars of flame erupt " +
        "periodically from vents in the floor. Fire elementals dance " +
        "through the flames, guardians of the dragon's domain. The heat " +
        "is almost unbearable, and sweat pours down your face."
    );

    add_exit("north", "/world/rooms/dragon/approach");
    add_exit("east", "/world/rooms/dragon/treasure_room");
    add_exit("south", "/world/rooms/dragon/lair");

    enable_reset(300);
    add_spawn("/world/mobs/fire_elemental");
    add_spawn("/world/mobs/fire_elemental");
}
