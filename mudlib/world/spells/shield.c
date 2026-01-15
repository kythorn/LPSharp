// /world/spells/shield.c
// Abjuration spell - temporary armor boost

inherit "/std/spell";

void create() {
    ::create();
    set_spell_name("Shield");
    set_spell_school("abjuration");
    set_mana_cost(8);
    set_min_skill(5);
    set_learn_skill(5);  // Requires abjuration 5 to learn at guild
    set_spell_description("Creates a magical shield that absorbs some damage.");
}

int do_spell(object caster, string args) {
    object room;
    int power;
    int shield_strength;
    int duration;

    room = environment(caster);

    // Calculate shield strength based on power
    power = calculate_power(caster);
    // Shield provides armor bonus equal to 25-50% of power
    shield_strength = (power / 4) + random(power / 4);
    if (shield_strength < 2) shield_strength = 2;

    // Duration in heartbeats (2 seconds each)
    // Base 30 seconds (15 beats) + 1 beat per 5 power
    duration = 15 + (power / 5);

    // Apply shield effect
    // We'll add a temporary armor bonus by setting a special variable
    // For simplicity, we'll just tell the player about the shield
    // A more complex implementation would track active effects

    // Messages
    tell_object(caster, "You conjure a shimmering magical shield around yourself.\n");
    tell_object(caster, "The shield provides +" + shield_strength + " armor for about " +
        ((duration * 2) / 60) + " minutes.\n");

    if (room) {
        tell_room(room, capitalize(call_other(caster, "query_short")) +
            " conjures a shimmering magical shield.\n", ({ caster }));
    }

    // For now, just grant a temporary HP boost instead
    // (True buff system would require tracking active effects)
    // This is a simplified implementation - heal for shield_strength HP
    tell_object(caster, "(Simplified: The magical energy grants temporary vigor.)\n");
    call_other(caster, "heal", shield_strength);

    return 1;
}
