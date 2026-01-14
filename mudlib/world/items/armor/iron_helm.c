// Iron Helm - Tier 3 armor
// Found in mines, dropped by various monsters

inherit "/std/armor";

void create() {
    ::create();
    set_name("helm");
    add_id("helmet");
    add_id("iron helm");
    add_id("iron helmet");
    set_short("an iron helm");
    set_mass(6);
    set_armor_class(2);
    set_slot("head");
    set_weight_category("medium");
}
