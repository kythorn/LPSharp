// /world/mobs/rat.c
// A small rat - non-aggressive starter monster
// Barehanded target: low HP, low damage

inherit "/std/monster";

void create() {
    ::create();
    set_short("a rat");
    set_name("rat");

    // Low stats - beatable by a new player
    set_str(1);
    set_dex(2);
    set_agi(2);
    set_con(1);

    // Override HP to be low (formula would give 15, we want 5)
    set_max_hp(5);
    set_hp(5);

    // Non-aggressive - player initiates combat
    set_aggressive(0);

    // Small XP reward
    set_xp_value(5);

    // Drop a rat tooth
    add_drop("/world/items/weapons/rat_tooth");
}
