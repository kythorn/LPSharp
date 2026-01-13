// Fire Elemental - Tier 5 monster
// Guards Dragon's Lair
// HP: 45, XP: 40

inherit "/std/monster";

void create() {
    ::create();

    set_name("fire elemental");
    set_short("a blazing fire elemental");

    set_str(5);
    set_dex(4);
    set_agi(4);
    set_con(7);  // HP = 10 + 7*5 = 45

    set_aggressive(1);
    set_xp_value(40);

    set_drop_chance(30);
    add_drop("/world/items/misc/fire_essence");
}
