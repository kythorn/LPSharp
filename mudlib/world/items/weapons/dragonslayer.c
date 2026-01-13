// Dragonslayer - Tier 6 Legendary weapon
// Dropped by the dragon boss

inherit "/std/weapon";

void create() {
    ::create();

    set_short("the legendary Dragonslayer sword");
    set_mass(18);
    set_damage(50);
    set_weapon_type("blade");
}

int id(string str) {
    if (str == "dragonslayer" || str == "sword" || str == "legendary sword") return 1;
    return ::id(str);
}
