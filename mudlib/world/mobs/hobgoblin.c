// Hobgoblin - Tier 4 monster
// Found in Goblin Caves (deeper areas)
// HP: 35, XP: 30

inherit "/std/monster";

void create() {
    ::create();

    set_name("hobgoblin");
    set_short("a hulking hobgoblin");

    set_str(4);
    set_dex(3);
    set_agi(2);
    set_con(5);  // HP = 10 + 5*5 = 35

    set_aggressive(1);
    set_xp_value(30);

    set_drop_chance(50);
    add_drop("/world/items/weapons/goblin_blade");
    add_drop("/world/items/armor/goblin_mail");
}
