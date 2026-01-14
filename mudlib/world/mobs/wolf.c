// /world/mobs/wolf.c
// A grey wolf - medium-hard difficulty

inherit "/std/monster";

void create() {
    ::create();
    set_short("a grey wolf");
    set_name("wolf");

    // Strong and fast predator
    set_str(4);
    set_dex(4);
    set_agi(4);
    set_con(4);

    // Good HP
    set_max_hp(25);
    set_hp(25);

    // Aggressive pack hunter
    set_aggressive(1);

    // Good XP reward
    set_xp_value(18);
}
