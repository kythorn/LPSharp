// /world/items/armor/spider_silk_gloves.c
// Delicate gloves woven from spider silk

inherit "/std/armor";

void create() {
    ::create();
    set_name("gloves");
    add_id("silk gloves");
    add_id("spider gloves");
    add_id("spider silk gloves");
    set_short("spider silk gloves");
    set_mass(1);
    set_armor_class(1);
    set_slot("hands");
    set_weight_category("none");
}
