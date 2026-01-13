// Dragon's Approach - Tier 6 area begins

inherit "/std/room";

void create() {
    ::create();

    set_short("Dragon's Approach");
    set_long(
        "The tunnel widens as you descend, and the heat becomes almost " +
        "unbearable. The walls here are scorched black, and an orange " +
        "glow emanates from ahead. You can feel the ground trembling " +
        "with the breathing of something massive. Scattered bones of " +
        "unfortunate adventurers line the passage - a warning to all " +
        "who would challenge what lies within."
    );

    add_exit("up", "/world/rooms/mines/flooded_shaft");
    add_exit("south", "/world/rooms/dragon/fire_cavern");

    add_spawn("/world/mobs/fire_elemental");
    enable_reset(300);
}
