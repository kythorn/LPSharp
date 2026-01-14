// /world/mobs/dire_wolf.c
// A massive dire wolf - forest boss

inherit "/std/monster";

void create() {
    ::create();
    set_short("a dire wolf");
    set_name("dire wolf");

    // Powerful boss monster
    set_str(7);
    set_dex(5);
    set_agi(5);
    set_con(6);

    // High HP for a boss
    set_max_hp(50);
    set_hp(50);

    // Very aggressive
    set_aggressive(1);

    // Boss drops a powerful fang weapon
    add_drop("/world/items/weapons/wolf_fang");
}
