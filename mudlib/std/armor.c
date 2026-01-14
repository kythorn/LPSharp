// /std/armor.c
// Base class for all armor

inherit "/std/object";

int armor_class;
string slot;
string weight_category;  // "none", "light", "medium", "heavy"

void create() {
    ::create();
    armor_class = 1;
    slot = "torso";
    weight_category = "light";  // Default to light armor
    set_mass(5);
}

// Weight category affects spell failure and dodge
// Categories: "none" (robes), "light" (leather), "medium" (chain), "heavy" (plate)
string query_weight_category() {
    return weight_category;
}

void set_weight_category(string cat) {
    weight_category = cat;
}

// Get spell failure chance based on weight category
int query_spell_failure() {
    if (weight_category == "none") return 0;
    if (weight_category == "light") return 10;
    if (weight_category == "medium") return 30;
    if (weight_category == "heavy") return 60;
    return 0;
}

// Get dodge penalty based on weight category
int query_dodge_penalty() {
    if (weight_category == "none") return 0;
    if (weight_category == "light") return 0;
    if (weight_category == "medium") return 10;
    if (weight_category == "heavy") return 25;
    return 0;
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
