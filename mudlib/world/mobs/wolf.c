// /world/mobs/wolf.c
// A fierce forest wolf - drops wolf pelt

inherit "/std/monster";

void create() {
    ::create();
    set_short("a fierce wolf");
    set_name("wolf");

    // Strong and tough
    set_str(3);
    set_dex(3);
    set_agi(2);
    set_con(3);

    set_max_hp(15);
    set_hp(15);

    // Aggressive - attacks on sight
    set_aggressive(1);

    set_xp_value(20);

    // Drop wolf pelt
    add_drop("/world/items/armor/wolf_pelt");
}
