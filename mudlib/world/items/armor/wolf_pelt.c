// /world/items/armor/wolf_pelt.c
// A thick wolf pelt that can be worn as armor

inherit "/std/armor";

void create() {
    ::create();
    set_name("pelt");
    add_id("wolf pelt");
    add_id("fur");
    add_id("wolf fur");
    set_short("a wolf pelt");
    set_armor_class(2);
    set_slot("torso");
    set_weight_category("light");
    set_mass(4);
}
