// ============================================================================
// EXAMPLE MONSTER - A template showing how to create monsters in LPMud Revival
// ============================================================================
//
// Monsters are NPCs that players can fight. They have stats, can drop items,
// and some are aggressive (attack players on sight).
//
// To create your own monster:
// 1. Copy this file to /world/mobs/yourmonster.c
// 2. Modify create() to customize stats, drops, and behavior
// 3. Add it to a room's spawn list to make it appear in the world
//
// ============================================================================

// Required: All monsters must inherit from /std/monster.c
inherit "/std/monster";

// ============================================================================
// create() - Called once when the monster is created
// ============================================================================

void create() {
    // IMPORTANT: Always call the parent's create() first!
    ::create();

    // -------------------------------------------------------------------------
    // BASIC IDENTITY
    // -------------------------------------------------------------------------

    // set_name(string) - The monster's name (no article)
    // This is what players type to target it: "attack goblin"
    set_name("example goblin");

    // set_short(string) - The display name with article
    // Shown in room descriptions and combat messages
    set_short("an example goblin");

    // -------------------------------------------------------------------------
    // STATS - Define the monster's power level
    // -------------------------------------------------------------------------
    // Stats affect combat:
    //   STR: Damage bonus (damage + STR/2)
    //   DEX: Hit chance (+3% per point)
    //   AGI: Dodge chance (enemies -2% per point)
    //   CON: Max HP (10 + CON*5)
    //   INT, WIS, CHA: Reserved for future magic/social systems

    set_str(3);    // Moderate strength
    set_dex(2);    // Below average accuracy
    set_agi(2);    // Slightly hard to hit
    set_con(4);    // HP will be 10 + 4*5 = 30
    set_int(1);
    set_wis(1);
    set_cha(1);

    // -------------------------------------------------------------------------
    // COMBAT BEHAVIOR
    // -------------------------------------------------------------------------

    // set_aggressive(0 or 1) - Does this monster attack on sight?
    //   0 = Passive - Only fights if attacked first
    //   1 = Aggressive - Attacks any player who enters the room
    //
    // Design tip: Make early monsters passive so new players can explore safely.
    // Make dangerous areas have aggressive monsters as a warning.
    set_aggressive(1);  // This goblin attacks on sight!

    // -------------------------------------------------------------------------
    // REWARDS
    // -------------------------------------------------------------------------

    // set_xp_value(int) - Experience points awarded when killed
    // Guidelines:
    //   5 XP    - Trivial (rat, beetle)
    //   10 XP   - Easy (spider, snake)
    //   15-20   - Normal (wolf, goblin)
    //   25-40   - Challenging (orc, troll)
    //   50-100  - Dangerous (ogre, demon)
    //   100+    - Boss monsters
    set_xp_value(20);

    // -------------------------------------------------------------------------
    // ITEM DROPS
    // -------------------------------------------------------------------------

    // Monsters can drop items when killed. Use add_drop() to add items to the
    // potential drop pool, and set_drop_chance() to set the probability.

    // set_drop_chance(percent) - Chance each item drops (0-100)
    // 100 = always drops, 50 = 50% chance, etc.
    set_drop_chance(50);

    // add_drop(path) - Add an item to the drop pool
    // Each item has an independent chance to drop based on drop_chance
    add_drop("/world/items/weapons/rusty_dagger");
    add_drop("/world/items/misc/gold_coins");

    // Alternative: Set all drops at once
    // set_drops(({ "/world/items/weapons/rusty_dagger", "/world/items/misc/gold_coins" }));
}

// ============================================================================
// init() - Called when something enters the monster's environment (optional)
// ============================================================================
// The base monster.c handles aggressive behavior automatically.
// Override only if you need special behavior.

void init() {
    ::init();  // Handles aggression

    // Example: Special greeting
    // if (this_player() && !attacker) {
    //     call_other(environment(), "act", this_object(),
    //         "", "The goblin eyes $N suspiciously.");
    // }
}

// ============================================================================
// die() - Called when the monster is killed (optional)
// ============================================================================
// The base monster.c handles XP rewards, corpse creation, and destruction.
// Override for special death effects.

void die() {
    // Example: Death message
    call_other(environment(), "act_all",
        "The goblin lets out a final shriek and collapses!\n",
        environment());

    // Call parent to handle drops, XP, corpse, and destruction
    ::die();
}

// ============================================================================
// MONSTER DESIGN GUIDELINES
// ============================================================================
//
// DIFFICULTY TIERS:
//
// Tier 1 - Beginner (HP 5-10, XP 5-10)
//   Example: Rat, Beetle, Small Spider
//   Stats: STR 1, DEX 2, AGI 2, CON 1
//   Passive, easy to kill, minimal threat
//   Drops: Trash items (teeth, wings) worth 1-5 damage
//
// Tier 2 - Easy (HP 10-20, XP 10-20)
//   Example: Spider, Snake, Goblin Scout
//   Stats: STR 2, DEX 3, AGI 2, CON 2-3
//   Passive or mildly aggressive
//   Drops: Basic items (AC 1-2 armor, 5-10 damage weapons)
//
// Tier 3 - Normal (HP 20-35, XP 20-35)
//   Example: Wolf, Goblin, Orc Grunt
//   Stats: STR 3, DEX 3, AGI 2, CON 3-5
//   Often aggressive, requires decent gear
//   Drops: Intermediate items (AC 2-3, 10-15 damage)
//
// Tier 4 - Challenging (HP 35-60, XP 35-60)
//   Example: Orc Warrior, Troll, Large Spider
//   Stats: STR 4-5, DEX 3-4, AGI 3, CON 5-8
//   Aggressive, requires good gear and strategy
//   Drops: Good items (AC 3-5, 15-25 damage)
//
// Tier 5 - Hard (HP 60-100, XP 60-100)
//   Example: Ogre, Demon, Dragon Whelp
//   Stats: STR 6+, DEX 4+, AGI 3+, CON 10+
//   Very dangerous, group content or well-geared
//   Drops: Rare/elite items (AC 5+, 25+ damage)
//
// Tier 6 - Boss (HP 100+, XP 100+)
//   Example: Dragon, Demon Lord, Ancient Lich
//   Unique, possibly with special abilities
//   Major drops, quest rewards
//
// SPAWN CONSIDERATIONS:
//   - Put tier 1-2 near starting areas
//   - Create paths of increasing difficulty
//   - Mix passive and aggressive in the same area
//   - Consider respawn timers (60s for easy, 120s+ for hard)
//
// ============================================================================
