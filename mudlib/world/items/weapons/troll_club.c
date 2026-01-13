// Troll Club - Tier 4 weapon
// Dropped by trolls

inherit "/std/weapon";

void create() {
    ::create();

    set_short("a massive troll club");
    set_mass(25);
    set_damage(28);
    set_weapon_type("blunt");
}

int id(string str) {
    if (str == "club" || str == "troll club" || str == "massive club") return 1;
    return ::id(str);
}
