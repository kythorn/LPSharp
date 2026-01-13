// Leather Armor - Tier 2 armor
// Basic torso armor, buyable in shops

inherit "/std/armor";

void create() {
    ::create();

    set_short("leather armor");
    set_mass(8);
    set_armor_class(2);
    set_slot("torso");
}

int id(string str) {
    if (str == "leather" || str == "leather armor" || str == "armor") return 1;
    return ::id(str);
}
