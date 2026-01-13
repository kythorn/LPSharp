// Goblin Blade - Tier 3 weapon
// Dropped by hobgoblins

inherit "/std/weapon";

void create() {
    ::create();

    set_short("a jagged goblin blade");
    set_mass(12);
    set_damage(18);
    set_weapon_type("blade");
}

int id(string str) {
    if (str == "blade" || str == "goblin blade" || str == "jagged blade") return 1;
    return ::id(str);
}
