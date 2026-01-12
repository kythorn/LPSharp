// /world/mobs/snake.c
// A venomous snake - drops snake fang

inherit "/std/monster";

void create() {
    ::create();
    set_short("a venomous snake");
    set_name("snake");

    // Quick but fragile
    set_str(2);
    set_dex(4);
    set_agi(3);
    set_con(2);

    set_max_hp(10);
    set_hp(10);

    // Non-aggressive unless provoked
    set_aggressive(0);

    set_xp_value(15);

    // Drop snake fang
    add_drop("/world/items/weapons/snake_fang");
}
