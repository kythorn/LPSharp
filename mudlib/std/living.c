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

// Mana pool
int mana;
int max_mana;

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

// Skills - mapping of skill_name to skill_value
mapping skills;

// Allowed skills (granted by guilds) - array of skill names
// If empty, all skills are allowed (for backwards compatibility with monsters)
string *allowed_skills;

// Known spells - array of spell paths this living has learned
string *known_spells;

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

    // Calculate and set Mana: 10 + (INT * 5)
    max_mana = 10 + (intelligence * 5);
    mana = max_mana;

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

    // Skills - start with empty skills
    skills = ([]);

    // Allowed skills - empty means all allowed (for monsters)
    // Players/guild members will have specific skills added
    allowed_skills = ({});

    // Known spells - empty initially
    known_spells = ({});
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

void set_int(int val) {
    intelligence = val;
    // Recalculate max mana when intelligence changes
    max_mana = 10 + (intelligence * 5);
    if (mana > max_mana) {
        mana = max_mana;
    }
}
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

// Get a description of health status based on HP percentage
string query_health_desc() {
    int pct;

    if (max_hp <= 0) return "in perfect health";

    pct = (hp * 100) / max_hp;

    if (pct >= 100) return "in perfect health";
    if (pct >= 90) return "slightly scratched";
    if (pct >= 75) return "bruised";
    if (pct >= 50) return "wounded";
    if (pct >= 25) return "badly wounded";
    if (pct >= 10) return "severely wounded";
    return "near death";
}

// Mana getters/setters
int query_mana() { return mana; }
int query_max_mana() { return max_mana; }

void set_mana(int val) {
    mana = val;
    if (mana > max_mana) {
        mana = max_mana;
    }
    if (mana < 0) {
        mana = 0;
    }
}

// Mana regen rate: 1 + (WIS / 3) per tick
int query_mana_regen() {
    return 1 + (wis / 3);
}

// Use mana for spell casting
// Returns 1 if enough mana, 0 if not
int use_mana(int cost) {
    if (mana < cost) {
        return 0;
    }
    mana = mana - cost;
    return 1;
}

// Restore mana
void restore_mana(int amount) {
    mana = mana + amount;
    if (mana > max_mana) {
        mana = max_mana;
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

// Calculate total spell failure chance from all worn armor
// Returns percentage (0-100+)
int query_total_spell_failure() {
    int total;
    string *slots;
    int i;

    total = 0;
    slots = keys(worn_armor);

    for (i = 0; i < sizeof(slots); i++) {
        object armor;
        armor = worn_armor[slots[i]];
        if (armor) {
            total = total + call_other(armor, "query_spell_failure");
        }
    }

    return total;
}

// Calculate total dodge penalty from all worn armor
// Returns percentage reduction to dodge effectiveness
int query_total_dodge_penalty() {
    int total;
    string *slots;
    int i;

    total = 0;
    slots = keys(worn_armor);

    for (i = 0; i < sizeof(slots); i++) {
        object armor;
        armor = worn_armor[slots[i]];
        if (armor) {
            total = total + call_other(armor, "query_dodge_penalty");
        }
    }

    return total;
}

// Get the skill type being used for current weapon
string query_weapon_skill() {
    if (wielded_weapon) {
        return call_other(wielded_weapon, "query_skill_type");
    }
    return "unarmed";
}

// Calculate damage based on weapon or unarmed, modified by skill
int query_damage() {
    int base_damage;
    int skill_value;
    string skill_name;

    if (wielded_weapon) {
        base_damage = call_other(wielded_weapon, "query_damage");
        skill_name = call_other(wielded_weapon, "query_skill_type");
    } else {
        // Unarmed damage: 1 + (str / 3)
        base_damage = 1 + (str / 3);
        skill_name = "unarmed";
    }

    // Get weapon skill value
    skill_value = query_skill(skill_name);

    // Add strength bonus: str / 2
    base_damage = base_damage + (str / 2);

    // Skill bonus: effective_damage = base * (1 + skill/50)
    // At skill 0: 100% damage
    // At skill 50: 200% damage
    // At skill 100: 300% damage
    int skill_multiplier;
    skill_multiplier = 100 + (skill_value * 2);
    base_damage = (base_damage * skill_multiplier) / 100;

    return base_damage;
}

// Calculate hit chance against a target
int query_hit_chance(object target) {
    int chance;
    int target_agi;
    int target_dodge;
    int target_dodge_penalty;
    int effective_dodge;
    int weapon_skill;
    string skill_name;

    // Get weapon skill
    skill_name = query_weapon_skill();
    weapon_skill = query_skill(skill_name);

    // Base 30% + dex * 2 + weapon_skill / 2
    // At skill 0, dex 1: 32%
    // At skill 50, dex 10: 30 + 20 + 25 = 75%
    chance = 30 + (dex * 2) + (weapon_skill / 2);

    // Subtract target's dodge (agi * 2 + dodge_skill / 3)
    // Dodge is reduced by armor penalty
    if (target) {
        target_agi = call_other(target, "query_agi");
        target_dodge = call_other(target, "query_skill", "dodge");
        target_dodge_penalty = call_other(target, "query_total_dodge_penalty");

        // Apply armor penalty to dodge skill effectiveness
        // At 25% penalty, dodge is reduced to 75% effectiveness
        effective_dodge = (target_dodge * (100 - target_dodge_penalty)) / 100;

        chance = chance - (target_agi * 2) - (effective_dodge / 3);
    }

    // Drunk penalty: lose 1% hit chance per 2 intoxication
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

    // Use mapping concatenation as workaround for index assignment
    worn_armor = worn_armor + ([ slot: armor ]);
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

// =============================================================================
// SKILL SYSTEM
// =============================================================================

// Basic skills available to everyone (no guild required)
string *query_basic_skills() {
    return ({ "unarmed", "dodge", "haggling", "swimming" });
}

// Check if a skill is allowed for this living (guild membership or basic skill)
int can_use_skill(string skill_name) {
    int i;
    string *basic;

    if (!skill_name || skill_name == "") {
        return 0;
    }

    // Basic skills are always allowed
    basic = query_basic_skills();
    for (i = 0; i < sizeof(basic); i++) {
        if (basic[i] == skill_name) {
            return 1;
        }
    }

    // If allowed_skills is empty, allow all (for monsters/NPCs)
    if (sizeof(allowed_skills) == 0) {
        return 1;
    }

    // Check if skill is in allowed list (from guild membership)
    for (i = 0; i < sizeof(allowed_skills); i++) {
        if (allowed_skills[i] == skill_name) {
            return 1;
        }
    }

    return 0;
}

// Get current skill value
int query_skill(string skill_name) {
    if (!skill_name || skill_name == "") {
        return 0;
    }

    if (!skills[skill_name]) {
        return 0;
    }

    return skills[skill_name];
}

// Set skill value directly (for admin/testing)
void set_skill(string skill_name, int value) {
    if (!skill_name || skill_name == "") {
        return;
    }

    if (value < 0) {
        value = 0;
    }

    // Use mapping concatenation as workaround for index assignment
    skills = skills + ([ skill_name: value ]);
}

// Advance a skill through use (with logarithmic diminishing returns)
// difficulty: scales chance based on challenge (higher = easier to learn)
// Returns 1 if skill increased, 0 if not
int advance_skill(string skill_name, int difficulty) {
    int current;
    int chance;
    int roll;
    int base_chance;

    if (!skill_name || skill_name == "") {
        return 0;
    }

    // Must have access to this skill
    if (!can_use_skill(skill_name)) {
        return 0;
    }

    current = query_skill(skill_name);

    // Base chance is 30% (tunable)
    base_chance = 30;

    // Logarithmic formula: chance = base_chance / (1 + current/10) * difficulty_mod
    // At skill 0: 30%
    // At skill 10: 15%
    // At skill 30: 7.5%
    // At skill 50: 5%
    // At skill 100: 2.7%

    // Calculate divisor: 1 + (current / 10)
    // This gives us: 1, 2, 4, 6, 11 at skills 0, 10, 30, 50, 100
    int divisor;
    divisor = 1 + (current / 10);

    chance = base_chance / divisor;

    // Apply difficulty modifier (default 10 = normal, 20 = easier, 5 = harder)
    if (difficulty <= 0) {
        difficulty = 10;
    }
    chance = (chance * difficulty) / 10;

    // Minimum 1% chance, maximum 50% chance
    if (chance < 1) {
        chance = 1;
    }
    if (chance > 50) {
        chance = 50;
    }

    // Roll for skill gain
    roll = random(100);
    if (roll < chance) {
        set_skill(skill_name, current + 1);
        return 1;
    }

    return 0;
}

// Add a skill to the allowed list (called by guilds)
void add_allowed_skill(string skill_name) {
    int i;

    if (!skill_name || skill_name == "") {
        return;
    }

    // Check if already allowed
    for (i = 0; i < sizeof(allowed_skills); i++) {
        if (allowed_skills[i] == skill_name) {
            return;
        }
    }

    allowed_skills = allowed_skills + ({ skill_name });
}

// Remove a skill from the allowed list (when leaving a guild)
void remove_allowed_skill(string skill_name) {
    int i;
    string *new_list;

    if (!skill_name || skill_name == "") {
        return;
    }

    new_list = ({});
    for (i = 0; i < sizeof(allowed_skills); i++) {
        if (allowed_skills[i] != skill_name) {
            new_list = new_list + ({ allowed_skills[i] });
        }
    }
    allowed_skills = new_list;
}

// Get all allowed skills
string *query_allowed_skills() {
    return allowed_skills;
}

// Get all skills (the mapping)
mapping query_skills() {
    return skills;
}

// =============================================================================
// SPELL SYSTEM
// =============================================================================

// Get all known spells
string *query_known_spells() {
    return known_spells;
}

// Check if this living knows a spell
int knows_spell(string spell_path) {
    int i;

    if (!spell_path || spell_path == "") {
        return 0;
    }

    for (i = 0; i < sizeof(known_spells); i++) {
        if (known_spells[i] == spell_path) {
            return 1;
        }
    }

    return 0;
}

// Learn a new spell
void learn_spell(string spell_path) {
    if (!spell_path || spell_path == "") {
        return;
    }

    if (!knows_spell(spell_path)) {
        known_spells = known_spells + ({ spell_path });
    }
}

// Forget a spell
void forget_spell(string spell_path) {
    int i;
    string *new_list;

    if (!spell_path || spell_path == "") {
        return;
    }

    new_list = ({});
    for (i = 0; i < sizeof(known_spells); i++) {
        if (known_spells[i] != spell_path) {
            new_list = new_list + ({ known_spells[i] });
        }
    }
    known_spells = new_list;
}

// Called when this living successfully dodges an attack
// Gives a chance to improve dodge skill
void try_dodge_advancement(int difficulty) {
    int skill_gained;

    skill_gained = advance_skill("dodge", difficulty);
    if (skill_gained) {
        tell_object(this_object(), "[Your dodge skill improves!]\n");
        advance_stats_for_skill("dodge");
    }
}

// =============================================================================
// STAT GROWTH SYSTEM
// =============================================================================

// Advance a stat through use (with logarithmic diminishing returns)
// Stats grow MUCH slower than skills
// Returns 1 if stat increased, 0 if not
int advance_stat(string stat_name) {
    int current;
    int chance;
    int roll;
    int base_chance;
    int divisor;

    // Base chance is 5% (stats grow slowly)
    base_chance = 5;

    // Get current stat value
    if (stat_name == "str") {
        current = str;
    } else if (stat_name == "dex") {
        current = dex;
    } else if (stat_name == "agi") {
        current = agi;
    } else if (stat_name == "con") {
        current = con;
    } else if (stat_name == "int") {
        current = intelligence;
    } else if (stat_name == "wis") {
        current = wis;
    } else if (stat_name == "cha") {
        current = cha;
    } else {
        return 0;
    }

    // Logarithmic formula: chance = base_chance / (1 + current/3)
    // At stat 1: 5% / 1.33 = 3.75%
    // At stat 3: 5% / 2 = 2.5%
    // At stat 9: 5% / 4 = 1.25%
    // At stat 15: 5% / 6 = 0.83%
    // At stat 21: 5% / 8 = 0.62%

    divisor = 1 + (current / 3);
    chance = base_chance * 100 / divisor;  // Multiply by 100 for precision
    chance = chance / 100;  // Now we have the percentage

    // Since we're dealing with very small percentages, use a larger scale
    // Roll out of 1000 instead of 100
    if (chance < 1) {
        // For very low chances, use per-mille instead
        // Recalculate: base 50 per mille, divided by (1 + current/3)
        int permille;
        permille = 50 / divisor;
        if (permille < 1) {
            permille = 1;  // Always at least 0.1% chance
        }
        roll = random(1000);
        if (roll < permille) {
            // Stat increased!
            if (stat_name == "str") {
                str = str + 1;
            } else if (stat_name == "dex") {
                dex = dex + 1;
            } else if (stat_name == "agi") {
                agi = agi + 1;
            } else if (stat_name == "con") {
                set_con(con + 1);  // Use setter to recalculate max HP
            } else if (stat_name == "int") {
                set_int(intelligence + 1);  // Use setter to recalculate max mana
            } else if (stat_name == "wis") {
                wis = wis + 1;
            } else if (stat_name == "cha") {
                cha = cha + 1;
            }
            return 1;
        }
    } else {
        roll = random(100);
        if (roll < chance) {
            // Stat increased!
            if (stat_name == "str") {
                str = str + 1;
            } else if (stat_name == "dex") {
                dex = dex + 1;
            } else if (stat_name == "agi") {
                agi = agi + 1;
            } else if (stat_name == "con") {
                set_con(con + 1);  // Use setter to recalculate max HP
            } else if (stat_name == "int") {
                set_int(intelligence + 1);  // Use setter to recalculate max mana
            } else if (stat_name == "wis") {
                wis = wis + 1;
            } else if (stat_name == "cha") {
                cha = cha + 1;
            }
            return 1;
        }
    }

    return 0;
}

// Helper to advance stats based on skill type
// Call this when a skill is used successfully
void advance_stats_for_skill(string skill_name) {
    // Combat skills
    if (skill_name == "sword" || skill_name == "axe" || skill_name == "mace" ||
        skill_name == "unarmed") {
        advance_stat("str");
        advance_stat("dex");
    }
    else if (skill_name == "dagger" || skill_name == "bow") {
        advance_stat("dex");
        advance_stat("agi");
    }
    else if (skill_name == "shield_block") {
        advance_stat("str");
        advance_stat("con");
    }
    else if (skill_name == "parry") {
        advance_stat("dex");
        advance_stat("agi");
    }
    else if (skill_name == "dodge") {
        advance_stat("agi");
    }
    // Magic schools
    else if (skill_name == "evocation" || skill_name == "conjuration" ||
             skill_name == "transmutation" || skill_name == "abjuration" ||
             skill_name == "divination" || skill_name == "illusion" ||
             skill_name == "enchantment" || skill_name == "necromancy") {
        advance_stat("int");
        advance_stat("wis");
    }
    // Thief skills
    else if (skill_name == "stealth" || skill_name == "lockpicking") {
        advance_stat("dex");
        advance_stat("agi");
    }
    // Social skills
    else if (skill_name == "haggling") {
        advance_stat("cha");
    }
    // Endurance skills
    else if (skill_name == "swimming" || skill_name == "climbing") {
        advance_stat("con");
        advance_stat("str");
    }
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

    // Don't check for death here - let the attacker handle it after showing damage
    if (hp <= 0) {
        hp = 0;
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
    string weapon_skill;
    int target_max_hp;
    int difficulty;
    int skill_gained;

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

    // Get weapon skill for advancement
    weapon_skill = query_weapon_skill();

    // Calculate difficulty based on target's max HP (proxy for strength)
    // Higher HP targets = more learning opportunity
    target_max_hp = call_other(attacker, "query_max_hp");
    difficulty = 5 + (target_max_hp / 5);  // 5-20+ difficulty range
    if (difficulty > 20) difficulty = 20;

    // Roll to hit
    hit_chance = query_hit_chance(attacker);
    hit_roll = random(100);

    if (hit_roll < hit_chance) {
        // Hit!
        damage = query_damage();
        actual_damage = call_other(attacker, "receive_damage", damage, this_object());

        // Try to advance weapon skill (better chance on hit)
        skill_gained = advance_skill(weapon_skill, difficulty);
        if (skill_gained) {
            tell_object(this_object(), "[Your " + weapon_skill + " skill improves!]\n");
            // Also try to advance related stats
            advance_stats_for_skill(weapon_skill);
        }

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

        // Check if target died (after showing damage message)
        if (call_other(attacker, "query_hp") <= 0) {
            call_other(attacker, "die");
            stop_combat();
        }
    } else {
        // Miss! Defender dodged - they might learn from it
        call_other(attacker, "try_dodge_advancement", difficulty);

        // Attacker still learns a little from combat (lower chance)
        skill_gained = advance_skill(weapon_skill, difficulty / 2);
        if (skill_gained) {
            tell_object(this_object(), "[Your " + weapon_skill + " skill improves!]\n");
            advance_stats_for_skill(weapon_skill);
        }

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

    // Not in combat - regenerate HP and mana
    int hp_full;
    int mana_full;

    hp_full = (hp >= max_hp);
    mana_full = (mana >= max_mana);

    // Regenerate HP
    if (!hp_full && (regen_rate > 0 || bonus_regen > 0)) {
        int total_regen;
        total_regen = regen_rate + bonus_regen;

        hp = hp + total_regen;
        if (hp > max_hp) {
            hp = max_hp;
        }

        // Notify player of healing (only when fully healed)
        if (hp >= max_hp) {
            tell_object(this_object(), "You are fully healed.\n");
            hp_full = 1;
        }
    }

    // Regenerate mana
    if (!mana_full) {
        int mana_regen;
        mana_regen = query_mana_regen();

        mana = mana + mana_regen;
        if (mana > max_mana) {
            mana = max_mana;
        }

        // Notify player when mana is full
        if (mana >= max_mana) {
            tell_object(this_object(), "Your mana is fully restored.\n");
            mana_full = 1;
        }
    }

    // Disable heartbeat when all resources are full and sober
    if (hp_full && mana_full && intoxication <= 0) {
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
