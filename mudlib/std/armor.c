// /std/armor.c
// Base class for all armor

inherit "/std/object";

int armor_class;
string slot;

void create() {
    ::create();
    armor_class = 1;
    slot = "torso";
    set_mass(5);
}

int query_armor_class() {
    return armor_class;
}

void set_armor_class(int ac) {
    armor_class = ac;
}

string query_slot() {
    return slot;
}

void set_slot(string s) {
    slot = s;
}

// Identify as armor
int is_armor() {
    return 1;
}
