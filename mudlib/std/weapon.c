// /std/weapon.c
// Base class for all weapons

inherit "/std/object";

int damage;
string weapon_type;

void create() {
    ::create();
    damage = 5;
    weapon_type = "melee";
    set_mass(10);
}

int query_damage() {
    return damage;
}

void set_damage(int d) {
    damage = d;
}

string query_weapon_type() {
    return weapon_type;
}

void set_weapon_type(string type) {
    weapon_type = type;
}
