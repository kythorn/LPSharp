// ============================================================================
// EXAMPLE WEAPON - A template showing how to create weapons in LPMud Revival
// ============================================================================
//
// Weapons increase damage dealt in combat. Players can wield one weapon at a
// time. Damage formula: weapon_damage + (player_STR / 2)
//
// To create your own weapon:
// 1. Copy this file to /world/items/weapons/yourweapon.c
// 2. Modify the create() function to customize the weapon
// 3. Test it: clone /world/items/weapons/yourweapon
//
// ============================================================================

// Required: All weapons must inherit from /std/weapon.c
inherit "/std/weapon";

// ============================================================================
// create() - Called once when the weapon is created
// ============================================================================

void create() {
    // IMPORTANT: Always call the parent's create() first!
    ::create();

    // -------------------------------------------------------------------------
    // BASIC PROPERTIES
    // -------------------------------------------------------------------------

    // set_short(string) - The weapon's name as shown in inventory
    // Should include "a/an" article
    set_short("an example sword");

    // set_mass(int) - The weight of the weapon
    // Affects inventory management (future feature)
    // Typical values: dagger 2, sword 10-15, greatsword 20-25
    set_mass(12);

    // -------------------------------------------------------------------------
    // COMBAT PROPERTIES
    // -------------------------------------------------------------------------

    // set_damage(int) - Base damage the weapon deals
    // This is added to STR/2 for total damage
    //
    // Guidelines for balance:
    //   1-5:   Very weak (rat tooth, broken knife)
    //   5-10:  Weak (dagger, club)
    //   10-15: Basic (short sword, mace)
    //   15-20: Good (longsword, battleaxe)
    //   20-30: Strong (greatsword, warhammer)
    //   30-50: Elite (enchanted weapons)
    //   50+:   Legendary
    set_damage(15);

    // set_weapon_type(string) - The type of weapon
    // Currently for flavor/display, may affect combat in future
    // Common types: "blade", "blunt", "piercing", "ranged"
    set_weapon_type("blade");
}

// ============================================================================
// id(string) - Recognition function (optional but recommended)
// ============================================================================
// This function is called when players try to interact with objects.
// Return 1 if the given string matches this object, 0 otherwise.
// The default checks query_short(), but you can add aliases here.

int id(string str) {
    // Match various names players might use
    if (str == "sword") return 1;
    if (str == "example sword") return 1;
    if (str == "example") return 1;

    // Fall back to default matching
    return ::id(str);
}

// ============================================================================
// BALANCING TIPS
// ============================================================================
//
// When creating weapons, consider the progression:
//
// TIER 1 (Beginner, ~5-15 damage)
//   - Rat Tooth (3 damage) - dropped by rats
//   - Snake Fang (5 damage) - dropped by snakes
//   - Rusty Sword (8 damage) - found/bought cheap
//   - Iron Dagger (10 damage) - basic shop weapon
//
// TIER 2 (Intermediate, ~15-25 damage)
//   - Iron Sword (15-20 damage) - standard shop weapon
//   - Steel Mace (18 damage) - blunt alternative
//   - Orc Cleaver (20 damage) - dropped by orcs
//
// TIER 3 (Advanced, ~25-40 damage)
//   - Fine Steel Sword (25 damage) - expensive shop weapon
//   - Troll Club (30 damage) - dropped by trolls
//   - Enchanted Blade (35 damage) - rare drop/quest reward
//
// TIER 4 (Elite, ~40-60 damage)
//   - Demon Slayer (45 damage) - boss drop
//   - Dragon's Bane (55 damage) - legendary quest reward
//
// Remember: Damage = weapon_damage + (STR / 2)
// A player with 10 STR adds 5 damage to every attack.
// A player with 20 STR adds 10 damage to every attack.
//
// Consider monster HP when balancing:
//   Rat: 5 HP      - should die in 1-2 hits with starter weapon
//   Spider: 8 HP   - 2-3 hits with tier 1 weapon
//   Wolf: 15 HP    - 3-4 hits with tier 1 weapon
//   Orc: 25 HP     - needs tier 2 weapon or good stats
//
// ============================================================================
