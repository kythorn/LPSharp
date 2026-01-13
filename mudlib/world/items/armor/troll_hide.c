// Troll Hide Armor - Tier 4 armor
// Dropped by trolls

inherit "/std/armor";

void create() {
    ::create();

    set_short("tough troll hide armor");
    set_mass(20);
    set_armor_class(6);
    set_slot("torso");
}

int id(string str) {
    if (str == "hide" || str == "troll hide" || str == "troll armor") return 1;
    return ::id(str);
}
