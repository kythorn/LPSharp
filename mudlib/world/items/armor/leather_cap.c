// /world/items/armor/leather_cap.c
// A simple leather cap

inherit "/std/armor";

void create() {
    ::create();
    set_short("a leather cap");
    set_armor_class(1);
    set_slot("head");
    set_mass(2);
}
