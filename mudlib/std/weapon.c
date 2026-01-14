// /std/weapon.c
// Base class for all weapons

inherit "/std/object";

int damage;
string weapon_type;
string skill_type;  // Which skill this weapon uses: sword, axe, mace, dagger, bow, etc.

void create() {
    ::create();
    damage = 5;
    weapon_type = "melee";
    skill_type = "sword";  // Default to sword
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

// Skill type determines which skill is used/advanced when fighting
// Valid values: sword, axe, mace, dagger, bow, unarmed
string query_skill_type() {
    return skill_type;
}

void set_skill_type(string type) {
    skill_type = type;
}

// Identify as a weapon
int is_weapon() {
    return 1;
}
