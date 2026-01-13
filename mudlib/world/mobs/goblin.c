// Goblin - Tier 3 monster
// Found in Goblin Caves
// HP: 20, XP: 15

inherit "/std/monster";

void create() {
    ::create();

    set_name("goblin");
    set_short("a snarling goblin");

    set_str(2);
    set_dex(3);
    set_agi(3);
    set_con(2);  // HP = 10 + 2*5 = 20

    set_aggressive(1);
    set_xp_value(15);

    set_drop_chance(40);
    add_drop("/world/items/weapons/rusty_dagger");
}
