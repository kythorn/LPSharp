// Spider Silk Gloves - Tier 3 armor
// Dropped by cave spiders

inherit "/std/armor";

void create() {
    ::create();

    set_short("spider silk gloves");
    set_mass(1);
    set_armor_class(2);
    set_slot("hands");
}

int id(string str) {
    if (str == "gloves" || str == "silk gloves" || str == "spider gloves" || str == "spider silk gloves") return 1;
    return ::id(str);
}
