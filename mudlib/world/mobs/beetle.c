// /world/mobs/beetle.c
// A large forest beetle - easy monster

inherit "/std/monster";

void create() {
    ::create();
    set_short("a large beetle");
    set_name("beetle");

    // Slow but has some armor (shell)
    set_str(2);
    set_dex(1);
    set_agi(1);
    set_con(3);

    // Moderate HP for its level
    set_max_hp(10);
    set_hp(10);

    // Non-aggressive
    set_aggressive(0);

    // Small XP reward
    set_xp_value(5);
}
