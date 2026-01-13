// Dragon - Tier 6 Boss monster
// Found in Dragon's Lair
// HP: 150, XP: 200

inherit "/std/monster";

void create() {
    ::create();

    set_name("dragon");
    set_short("an ancient fire dragon");

    set_str(10);
    set_dex(4);
    set_agi(3);
    set_con(28);  // HP = 10 + 28*5 = 150

    set_aggressive(1);
    set_xp_value(200);

    set_drop_chance(100);  // Boss always drops
    add_drop("/world/items/weapons/dragonslayer");
    add_drop("/world/items/armor/dragonscale");
}

void die() {
    // Epic death message
    call_other(environment(), "act_all",
        "With a thunderous roar, the ancient dragon crashes to the ground!\n" +
        "The earth shakes as the mighty beast breathes its last. Treasure\n" +
        "glitters among its hoard - the rewards of a true hero!\n",
        environment());

    ::die();
}
