// Leather Armor - Tier 2 armor
// Basic torso armor, buyable in shops

inherit "/std/armor";

void create() {
    ::create();
    set_name("armor");
    add_id("leather");
    add_id("leather armor");
    set_short("leather armor");
    set_mass(8);
    set_armor_class(2);
    set_slot("torso");
    set_weight_category("light");
}
