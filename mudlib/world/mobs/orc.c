// /world/mobs/orc.c
// An orc warrior - aggressive mid-level monster

inherit "/std/monster";

void create() {
    ::create();
    set_short("an orc");
    set_name("orc");

    // Medium stats
    set_str(3);
    set_dex(2);
    set_agi(1);
    set_con(3);

    // Recalculate HP with new constitution
    // con 3 = 10 + (3 * 5) = 25 HP
    set_max_hp(25);
    set_hp(25);

    // Aggressive - attacks on sight
    set_aggressive(1);

    // Moderate XP reward
    set_xp_value(25);
}
