// Dragonscale Armor - Tier 6 Legendary armor
// Dropped by the dragon boss

inherit "/std/armor";

void create() {
    ::create();
    set_name("dragonscale");
    add_id("armor");
    add_id("dragonscale armor");
    add_id("dragon armor");
    set_short("magnificent dragonscale armor");
    set_mass(22);
    set_armor_class(10);
    set_slot("torso");
    set_weight_category("heavy");
}
