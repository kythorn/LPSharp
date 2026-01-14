// /world/mobs/snake.c
// A venomous snake - medium difficulty

inherit "/std/monster";

void create() {
    ::create();
    set_short("a venomous snake");
    set_name("snake");

    // Fast and agile
    set_str(2);
    set_dex(5);
    set_agi(4);
    set_con(2);

    // Medium HP
    set_max_hp(15);
    set_hp(15);

    // Aggressive! Will attack on sight
    set_aggressive(1);

    // Moderate XP reward
    set_xp_value(10);
}
