// Rusty Dagger - Tier 2 weapon
// Dropped by goblins

inherit "/std/weapon";

void create() {
    ::create();

    set_short("a rusty dagger");
    set_mass(3);
    set_damage(8);
    set_weapon_type("piercing");
}

int id(string str) {
    if (str == "dagger" || str == "rusty dagger") return 1;
    return ::id(str);
}
