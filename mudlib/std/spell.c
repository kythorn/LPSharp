// /std/spell.c
// Base class for all spells

string spell_name;          // Display name
string spell_school;        // Required magic school (evocation, abjuration, etc.)
int mana_cost;              // Mana required to cast
int min_skill;              // Minimum school skill required to CAST
int learn_skill;            // Minimum school skill required to LEARN (at guild trainer)
string spell_description;   // Description of what the spell does

void create() {
    spell_name = "Unknown Spell";
    spell_school = "evocation";
    mana_cost = 10;
    min_skill = 0;
    learn_skill = 0;  // Skill required to learn at guild (0 = novice)
    spell_description = "An unknown magical effect.";
}

// Getters
string query_spell_name() { return spell_name; }
string query_spell_school() { return spell_school; }
int query_mana_cost() { return mana_cost; }
int query_min_skill() { return min_skill; }
int query_learn_skill() { return learn_skill; }
string query_spell_description() { return spell_description; }

// Setters
void set_spell_name(string n) { spell_name = n; }
void set_spell_school(string s) { spell_school = s; }
void set_mana_cost(int c) { mana_cost = c; }
void set_min_skill(int s) { min_skill = s; }
void set_learn_skill(int s) { learn_skill = s; }
void set_spell_description(string d) { spell_description = d; }

// Calculate spell power based on caster's skill and INT
// Higher skill and INT = more powerful spell
int calculate_power(object caster) {
    int skill;
    int intelligence;
    int power;

    if (!caster) return 10;

    skill = call_other(caster, "query_skill", spell_school);
    intelligence = call_other(caster, "query_int");

    // Base power of 10, scales with skill and INT
    // power = 10 + skill + (INT / 2)
    power = 10 + skill + (intelligence / 2);

    return power;
}

// Check if caster can cast this spell
// Returns 1 if allowed, 0 if not (with message to caster)
int can_cast(object caster, string args) {
    int skill;
    int spell_failure;
    int mana_available;

    if (!caster) return 0;

    // Check if caster has the required school skill allowed
    if (!call_other(caster, "can_use_skill", spell_school)) {
        tell_object(caster, "You don't know the " + spell_school + " school of magic.\n");
        return 0;
    }

    // Check minimum skill requirement
    skill = call_other(caster, "query_skill", spell_school);
    if (skill < min_skill) {
        tell_object(caster, "You need at least " + min_skill + " " + spell_school +
            " skill to cast " + spell_name + ".\n");
        return 0;
    }

    // Check mana
    mana_available = call_other(caster, "query_mana");
    if (mana_available < mana_cost) {
        tell_object(caster, "You don't have enough mana to cast " + spell_name +
            ". (Need " + mana_cost + ", have " + mana_available + ")\n");
        return 0;
    }

    // All checks passed
    return 1;
}

// Execute the spell
// Override this in subclasses to implement actual spell effect
// Returns 1 on success, 0 on failure
// Note: Mana is consumed BEFORE this is called, even if spell fails due to armor
int do_spell(object caster, string args) {
    tell_object(caster, "The spell fizzles with no effect.\n");
    return 0;
}

// Main cast function - called by the cast command
// Handles mana cost, spell failure, skill advancement
// Returns 1 on success, 0 on failure
int cast(object caster, string args) {
    int spell_failure;
    int roll;
    int difficulty;
    int skill_gained;

    if (!caster) return 0;

    // Check if we can cast
    if (!can_cast(caster, args)) {
        return 0;
    }

    // Consume mana first (even if spell might fail from armor)
    call_other(caster, "use_mana", mana_cost);

    // Check spell failure from armor
    spell_failure = call_other(caster, "query_total_spell_failure");
    if (spell_failure > 0) {
        roll = random(100);
        if (roll < spell_failure) {
            tell_object(caster, "Your armor interferes with the spell! The magic fizzles.\n");
            // Still get skill practice from failed cast (but reduced)
            difficulty = 5;  // Low difficulty for failed cast
            skill_gained = call_other(caster, "advance_skill", spell_school, difficulty);
            if (skill_gained) {
                tell_object(caster, "[Your " + spell_school + " skill improves!]\n");
                call_other(caster, "advance_stats_for_skill", spell_school);
            }
            return 0;
        }
    }

    // Execute the spell
    if (do_spell(caster, args)) {
        // Successful cast - try to advance skill
        // Difficulty based on spell's min_skill requirement
        difficulty = 10 + min_skill;
        if (difficulty > 25) difficulty = 25;

        skill_gained = call_other(caster, "advance_skill", spell_school, difficulty);
        if (skill_gained) {
            tell_object(caster, "[Your " + spell_school + " skill improves!]\n");
            call_other(caster, "advance_stats_for_skill", spell_school);
        }
        return 1;
    }

    return 0;
}

// Display spell info
string query_spell_info() {
    return sprintf(
        "%s (%s)\n" +
        "  Mana cost: %d\n" +
        "  Minimum skill: %d\n" +
        "  %s\n",
        spell_name, spell_school, mana_cost, min_skill, spell_description
    );
}
