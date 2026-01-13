// Cave Spider - Tier 4 monster
// Found in Dark Mines
// HP: 30, XP: 25

inherit "/std/monster";

void create() {
    ::create();

    set_name("cave spider");
    set_short("a giant cave spider");

    set_str(3);
    set_dex(5);
    set_agi(4);
    set_con(4);  // HP = 10 + 4*5 = 30

    set_aggressive(1);
    set_xp_value(25);

    set_drop_chance(40);
    add_drop("/world/items/armor/spider_silk_gloves");
}
