// Troll - Tier 5 monster
// Found in Dark Mines
// HP: 60, XP: 50

inherit "/std/monster";

void create() {
    ::create();

    set_name("troll");
    set_short("a massive cave troll");

    set_str(6);
    set_dex(2);
    set_agi(1);
    set_con(10);  // HP = 10 + 10*5 = 60

    set_aggressive(1);
    set_xp_value(50);

    set_drop_chance(60);
    add_drop("/world/items/weapons/troll_club");
    add_drop("/world/items/armor/troll_hide");
}
