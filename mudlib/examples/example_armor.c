// ============================================================================
// EXAMPLE ARMOR - A template showing how to create armor in LPMud Revival
// ============================================================================
//
// Armor reduces damage taken in combat. Players can wear multiple pieces in
// different slots. Damage reduction: incoming_damage - total_armor_class
// (minimum 1 damage always gets through)
//
// To create your own armor:
// 1. Copy this file to /world/items/armor/yourarmor.c
// 2. Modify the create() function to customize the armor
// 3. Test it: clone /world/items/armor/yourarmor
//
// ============================================================================

// Required: All armor must inherit from /std/armor.c
inherit "/std/armor";

// ============================================================================
// create() - Called once when the armor is created
// ============================================================================

void create() {
    // IMPORTANT: Always call the parent's create() first!
    ::create();

    // -------------------------------------------------------------------------
    // BASIC PROPERTIES
    // -------------------------------------------------------------------------

    // set_short(string) - The armor's name as shown in inventory
    set_short("an example helmet");

    // set_mass(int) - The weight of the armor
    // Typical values: gloves 1-2, cap 2-3, robes 5-8, chainmail 15-20, plate 25-30
    set_mass(4);

    // -------------------------------------------------------------------------
    // ARMOR PROPERTIES
    // -------------------------------------------------------------------------

    // set_armor_class(int) - How much damage this armor prevents
    // Total armor is the sum of all equipped pieces.
    //
    // Guidelines for balance:
    //   1-2:   Light (cloth, leather)
    //   3-5:   Medium (studded leather, chainmail)
    //   6-8:   Heavy (scale mail, plate)
    //   9-12:  Elite (magical armor)
    //   13+:   Legendary
    set_armor_class(3);

    // set_slot(string) - Which body slot this armor occupies
    // Players can wear one item per slot.
    //
    // Standard slots:
    //   "head"   - Helmets, caps, hoods, circlets
    //   "torso"  - Shirts, robes, chainmail, breastplates
    //   "hands"  - Gloves, gauntlets, bracers
    //   "legs"   - Pants, greaves, leg armor (future)
    //   "feet"   - Boots, shoes, sandals (future)
    //   "cloak"  - Cloaks, capes (future)
    //   "neck"   - Amulets, necklaces (future)
    //   "finger" - Rings (future, might allow 2)
    set_slot("head");
}

// ============================================================================
// id(string) - Recognition function (optional but recommended)
// ============================================================================

int id(string str) {
    if (str == "helmet") return 1;
    if (str == "example helmet") return 1;
    if (str == "example") return 1;
    if (str == "helm") return 1;

    return ::id(str);
}

// ============================================================================
// BALANCING TIPS
// ============================================================================
//
// Armor progression by slot:
//
// HEAD (AC 1-4)
//   - Leather Cap (AC 1) - basic, cheap
//   - Iron Helm (AC 2) - common
//   - Steel Helm (AC 3) - quality
//   - Enchanted Crown (AC 4) - rare
//
// TORSO (AC 2-8)
//   - Cloth Shirt (AC 1) - starter
//   - Leather Armor (AC 2) - basic
//   - Wolf Pelt (AC 2) - monster drop
//   - Chainmail (AC 4) - intermediate
//   - Scale Mail (AC 5) - good
//   - Plate Armor (AC 7) - heavy/expensive
//   - Dragon Scale (AC 8) - elite
//
// HANDS (AC 1-3)
//   - Web Gloves (AC 1) - spider drop
//   - Leather Gloves (AC 1) - basic
//   - Chain Gauntlets (AC 2) - intermediate
//   - Plate Gauntlets (AC 3) - heavy
//
// FULL GEAR TOTALS:
//   Tier 1 (Beginner): 3-5 total AC
//     Leather cap (1) + Leather armor (2) + Web gloves (1) = 4 AC
//
//   Tier 2 (Intermediate): 6-9 total AC
//     Iron helm (2) + Chainmail (4) + Chain gauntlets (2) = 8 AC
//
//   Tier 3 (Advanced): 10-14 total AC
//     Steel helm (3) + Plate armor (7) + Plate gauntlets (3) = 13 AC
//
// Consider monster damage when balancing:
//   With 5 AC, you reduce rat damage by 5, likely taking 0-1 damage
//   With 5 AC vs orc (stronger attack), you still take significant damage
//   Armor should make easier content trivial, but not trivialize hard content
//
// ============================================================================
