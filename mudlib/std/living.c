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

// Regeneration (HP per heartbeat when not in combat)
int regen_rate;

// Intoxication (0-100, affects regen and combat)
int intoxication;

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

    // Regeneration - 1 HP per heartbeat (2 seconds) when not in combat
    regen_rate = 1;

    // Start sober
    intoxication = 0;

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

// Intoxication functions
int query_intoxication() {
    return intoxication;
}

// Add intoxication from drinking
// Returns new intoxication level
int add_intoxication(int amount) {
    intoxication = intoxication + amount;
    if (intoxication > 100) {
        intoxication = 100;
    }
    if (intoxication < 0) {
        intoxication = 0;
    }

    // Start heartbeat for sobering up and boosted regen
    if (intoxication > 0) {
        set_heart_beat(1);
    }

    return intoxication;
}

// Check if too drunk to fight effectively
int is_too_drunk() {
    return intoxication >= 50;
}

// Get intoxication status message
string query_intoxication_status() {
    if (intoxication == 0) {
        return "sober";
    } else if (intoxication < 20) {
        return "tipsy";
    } else if (intoxication < 40) {
        return "buzzed";
    } else if (intoxication < 60) {
        return "drunk";
    } else if (intoxication < 80) {
        return "very drunk";
    } else {
        return "completely smashed";
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

    // Drunk penalty: lose 1% hit chance per 2 intoxication
    // At 50 intox (too drunk): -25% hit chance
    // At 100 intox (smashed): -50% hit chance
    if (intoxication > 0) {
        chance = chance - (intoxication / 2);
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

// Start combat with a target
void start_combat(object target) {
    if (!target) {
        return;
    }

    // Don't attack yourself
    if (target == this_object()) {
        return;
    }

    // Don't attack non-living things
    if (!call_other(target, "is_living")) {
        return;
    }

    // Already fighting this target
    if (attacker == target && in_combat) {
        return;
    }

    // Warn if drunk
    if (is_too_drunk()) {
        tell_object(this_object(), "You stagger drunkenly into combat!\n");
    }

    attacker = target;
    in_combat = 1;

    // Start heartbeat for combat rounds
    set_heart_beat(1);

    // Make target fight back
    if (!call_other(target, "query_in_combat")) {
        call_other(target, "start_combat", this_object());
    }
}

// Stop combat
void stop_combat() {
    in_combat = 0;
    attacker = 0;
    // Keep heartbeat running if we need to regenerate
    if (hp >= max_hp) {
        set_heart_beat(0);
    }
}

// Receive damage from an attacker
// Returns actual damage taken
int receive_damage(int amount, object from) {
    int actual;
    int armor_value;

    // Apply armor reduction
    armor_value = query_total_armor();
    actual = amount - armor_value;

    // Always do at least 1 damage
    if (actual < 1) {
        actual = 1;
    }

    hp = hp - actual;

    // Check for death
    if (hp <= 0) {
        hp = 0;
        die();
    } else {
        // Show HP status to the damaged creature
        int pct;
        pct = (hp * 100) / max_hp;
        if (pct <= 25) {
            tell_object(this_object(), "[HP: " + hp + "/" + max_hp + " - Near death!]\n");
        } else if (pct <= 50) {
            tell_object(this_object(), "[HP: " + hp + "/" + max_hp + " - Badly wounded]\n");
        } else if (pct <= 75) {
            tell_object(this_object(), "[HP: " + hp + "/" + max_hp + "]\n");
        }
        // Don't spam at high HP
    }

    return actual;
}

// Execute one attack against current target
void do_attack() {
    int hit_roll;
    int hit_chance;
    int damage;
    int actual_damage;
    string my_name;
    string target_name;
    object room;

    if (!attacker || !in_combat) {
        stop_combat();
        return;
    }

    // Check if attacker is still valid and in same room
    if (!attacker || environment(attacker) != environment(this_object())) {
        stop_combat();
        return;
    }

    // Check if attacker is dead
    if (call_other(attacker, "query_hp") <= 0) {
        stop_combat();
        return;
    }

    room = environment(this_object());
    my_name = query_short();
    target_name = call_other(attacker, "query_short");

    // Roll to hit
    hit_chance = query_hit_chance(attacker);
    hit_roll = random(100);

    if (hit_roll < hit_chance) {
        // Hit!
        damage = query_damage();
        actual_damage = call_other(attacker, "receive_damage", damage, this_object());

        // Messages
        tell_object(this_object(), "You hit " + target_name + " for " + actual_damage + " damage.\n");
        tell_object(attacker, capitalize(my_name) + " hits you for " + actual_damage + " damage.\n");

        // Tell room (excluding combatants)
        if (room) {
            object *others;
            int i;
            others = all_inventory(room);
            for (i = 0; i < sizeof(others); i++) {
                if (others[i] != this_object() && others[i] != attacker) {
                    if (call_other(others[i], "is_living")) {
                        tell_object(others[i], capitalize(my_name) + " hits " + target_name + ".\n");
                    }
                }
            }
        }
    } else {
        // Miss!
        tell_object(this_object(), "You miss " + target_name + ".\n");
        tell_object(attacker, capitalize(my_name) + " misses you.\n");

        // Tell room
        if (room) {
            object *others;
            int i;
            others = all_inventory(room);
            for (i = 0; i < sizeof(others); i++) {
                if (others[i] != this_object() && others[i] != attacker) {
                    if (call_other(others[i], "is_living")) {
                        tell_object(others[i], capitalize(my_name) + " misses " + target_name + ".\n");
                    }
                }
            }
        }
    }
}

// Heartbeat - called every 2 seconds
// Handles combat rounds and regeneration
void heart_beat() {
    int bonus_regen;
    int was_drunk;

    // If in combat, execute an attack
    if (in_combat && attacker) {
        do_attack();
        // Still process sobering while fighting
        if (intoxication > 0) {
            intoxication = intoxication - 2;
            if (intoxication < 0) intoxication = 0;
        }
        return;
    }

    // Track if we were drunk before sobering
    was_drunk = intoxication;

    // Sober up over time (2 points per heartbeat = ~1 minute to sober from one drink)
    if (intoxication > 0) {
        intoxication = intoxication - 2;
        if (intoxication <= 0) {
            intoxication = 0;
            if (was_drunk > 0) {
                tell_object(this_object(), "You feel sober again.\n");
            }
        }
    }

    // Calculate bonus regen from intoxication (intoxication/10)
    bonus_regen = intoxication / 10;

    // Not in combat - regenerate HP
    if (hp < max_hp && (regen_rate > 0 || bonus_regen > 0)) {
        int total_regen;
        total_regen = regen_rate + bonus_regen;

        hp = hp + total_regen;
        if (hp > max_hp) {
            hp = max_hp;
        }

        // Notify player of healing (only when fully healed)
        if (hp == max_hp) {
            tell_object(this_object(), "You are fully healed.\n");
            // Don't disable heartbeat if still intoxicated
            if (intoxication <= 0) {
                set_heart_beat(0);
            }
        }
    } else if (intoxication <= 0) {
        // Full HP and sober, disable heartbeat
        set_heart_beat(0);
    }
}

// Virtual die function - override in subclasses
void die() {
    // Base implementation - stop combat
    stop_combat();

    // Notify room
    object room;
    room = environment(this_object());
    if (room) {
        tell_room(room, capitalize(query_short()) + " dies.\n");
    }
}
