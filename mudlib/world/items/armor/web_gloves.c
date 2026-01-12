// /world/items/armor/web_gloves.c
// Gloves woven from spider silk

inherit "/std/armor";

void create() {
    ::create();
    set_short("a pair of web gloves");
    set_armor_class(1);
    set_slot("hands");
    set_mass(1);
}
