// /world/items/armor/beetle_carapace.c
// A hardened beetle shell - basic starting armor

inherit "/std/armor";

void create() {
    ::create();
    set_name("carapace");
    add_id("beetle carapace");
    add_id("shell");
    add_id("beetle shell");
    set_short("a beetle carapace");
    set_armor_class(1);
    set_slot("torso");
    set_weight_category("light");
    set_mass(3);
}
