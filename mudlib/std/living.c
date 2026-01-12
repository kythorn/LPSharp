// /std/living.c
// Base class for all living things (players, monsters)
// Provides stats, HP, and combat capabilities

inherit "/std/object";

// Stats - all default to 1
int str;
int dex;
int agi;
int con;
int intelligence;  // "int" is a reserved keyword
int wis;
int cha;

// Hit points
int hp;
int max_hp;

// Combat state
object attacker;
int in_combat;

// Equipment
object wielded_weapon;
mapping worn_armor;

void create() {
    ::create();

    // Initialize stats to 1
    str = 1;
    dex = 1;
    agi = 1;
    con = 1;
    intelligence = 1;
    wis = 1;
    cha = 1;

    // Calculate and set HP
    max_hp = 10 + (con * 5);
    hp = max_hp;

    // Combat state
    attacker = 0;
    in_combat = 0;

    // Equipment
    wielded_weapon = 0;
    worn_armor = ([]);
}

// Identify as a living thing
int is_living() {
    return 1;
}

// Stat getters
int query_str() { return str; }
int query_dex() { return dex; }
int query_agi() { return agi; }
int query_con() { return con; }
int query_int() { return intelligence; }
int query_wis() { return wis; }
int query_cha() { return cha; }

// Stat setters
void set_str(int val) { str = val; }
void set_dex(int val) { dex = val; }
void set_agi(int val) { agi = val; }

void set_con(int val) {
    con = val;
    // Recalculate max HP when constitution changes
    max_hp = 10 + (con * 5);
    if (hp > max_hp) {
        hp = max_hp;
    }
}

void set_int(int val) { intelligence = val; }
void set_wis(int val) { wis = val; }
void set_cha(int val) { cha = val; }

// HP getters/setters
int query_hp() { return hp; }
int query_max_hp() { return max_hp; }

void set_hp(int val) {
    hp = val;
    if (hp > max_hp) {
        hp = max_hp;
    }
    if (hp < 0) {
        hp = 0;
    }
}

void set_max_hp(int val) {
    max_hp = val;
    if (hp > max_hp) {
        hp = max_hp;
    }
}

// Heal HP by amount
void heal(int amount) {
    hp = hp + amount;
    if (hp > max_hp) {
        hp = max_hp;
    }
}

// Combat state getters
int query_in_combat() { return in_combat; }
object query_attacker() { return attacker; }

// Equipment getters
object query_wielded() { return wielded_weapon; }
mapping query_worn_armor() { return worn_armor; }

// Calculate total armor value from all worn pieces
int query_total_armor() {
    int total;
    string *slots;
    int i;

    total = 0;
    slots = keys(worn_armor);

    for (i = 0; i < sizeof(slots); i++) {
        object armor;
        armor = worn_armor[slots[i]];
        if (armor) {
            total = total + call_other(armor, "query_armor_class");
        }
    }

    return total;
}

// Calculate damage based on weapon or unarmed
int query_damage() {
    int base_damage;

    if (wielded_weapon) {
        base_damage = call_other(wielded_weapon, "query_damage");
    } else {
        // Unarmed damage: 1 + (str / 3)
        base_damage = 1 + (str / 3);
    }

    // Add strength bonus: str / 2
    return base_damage + (str / 2);
}

// Calculate hit chance against a target
int query_hit_chance(object target) {
    int chance;
    int target_agi;

    // Base 50% + dex * 3
    chance = 50 + (dex * 3);

    // Subtract target's dodge (agi * 2)
    if (target) {
        target_agi = call_other(target, "query_agi");
        chance = chance - (target_agi * 2);
    }

    // Clamp to reasonable range
    if (chance < 5) {
        chance = 5;
    }
    if (chance > 95) {
        chance = 95;
    }

    return chance;
}

// Wield a weapon
int wield_weapon(object weapon) {
    if (!weapon) {
        return 0;
    }

    // Check if it's a weapon
    if (!call_other(weapon, "is_weapon")) {
        return 0;
    }

    // Unwield current weapon if any
    if (wielded_weapon) {
        unwield_weapon();
    }

    wielded_weapon = weapon;
    return 1;
}

// Unwield current weapon
int unwield_weapon() {
    if (!wielded_weapon) {
        return 0;
    }

    wielded_weapon = 0;
    return 1;
}

// Wear armor in a slot
int wear_armor(object armor) {
    string slot;

    if (!armor) {
        return 0;
    }

    // Check if it's armor
    if (!call_other(armor, "is_armor")) {
        return 0;
    }

    slot = call_other(armor, "query_slot");
    if (!slot || slot == "") {
        return 0;
    }

    // Check if slot is already occupied
    if (worn_armor[slot]) {
        return 0;
    }

    worn_armor[slot] = armor;
    return 1;
}

// Remove armor from a slot
int remove_armor(string slot) {
    if (!slot || slot == "") {
        return 0;
    }

    if (!worn_armor[slot]) {
        return 0;
    }

    worn_armor = m_delete(worn_armor, slot);
    return 1;
}

// Remove specific armor object
int remove_armor_obj(object armor) {
    string *slots;
    int i;

    if (!armor) {
        return 0;
    }

    slots = keys(worn_armor);
    for (i = 0; i < sizeof(slots); i++) {
        if (worn_armor[slots[i]] == armor) {
            worn_armor = m_delete(worn_armor, slots[i]);
            return 1;
        }
    }

    return 0;
}
