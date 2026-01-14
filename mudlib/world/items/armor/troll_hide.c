// Troll Hide Armor - Tier 4 armor
// Dropped by trolls

inherit "/std/armor";

void create() {
    ::create();
    set_name("hide");
    add_id("armor");
    add_id("troll hide");
    add_id("troll armor");
    set_short("tough troll hide armor");
    set_mass(20);
    set_armor_class(6);
    set_slot("torso");
    set_weight_category("medium");
}
