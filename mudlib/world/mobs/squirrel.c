// /world/mobs/squirrel.c
// A quick squirrel - very easy but fast

inherit "/std/monster";

void create() {
    ::create();
    set_short("a squirrel");
    set_name("squirrel");

    // Fast but weak
    set_str(1);
    set_dex(4);
    set_agi(5);
    set_con(1);

    // Very low HP
    set_max_hp(4);
    set_hp(4);

    // Non-aggressive
    set_aggressive(0);

    // Tiny XP reward
    set_xp_value(3);
}
