// /world/mobs/spider.c
// A large forest spider - medium difficulty

inherit "/std/monster";

void create() {
    ::create();
    set_short("a large spider");
    set_name("spider");

    // Quick and dangerous
    set_str(3);
    set_dex(4);
    set_agi(3);
    set_con(3);

    // Medium HP
    set_max_hp(18);
    set_hp(18);

    // Aggressive
    set_aggressive(1);

    // Moderate XP reward
    set_xp_value(12);
}
