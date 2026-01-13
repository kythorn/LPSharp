// Iron Helm - Tier 3 armor
// Found in mines, dropped by various monsters

inherit "/std/armor";

void create() {
    ::create();

    set_short("an iron helm");
    set_mass(6);
    set_armor_class(2);
    set_slot("head");
}

int id(string str) {
    if (str == "helm" || str == "helmet" || str == "iron helm" || str == "iron helmet") return 1;
    return ::id(str);
}
