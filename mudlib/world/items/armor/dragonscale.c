// Dragonscale Armor - Tier 6 Legendary armor
// Dropped by the dragon boss

inherit "/std/armor";

void create() {
    ::create();

    set_short("magnificent dragonscale armor");
    set_mass(22);
    set_armor_class(10);
    set_slot("torso");
}

int id(string str) {
    if (str == "dragonscale" || str == "armor" || str == "dragonscale armor" || str == "dragon armor") return 1;
    return ::id(str);
}
