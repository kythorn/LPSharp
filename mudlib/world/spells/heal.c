// /world/spells/heal.c
// Abjuration spell - restore HP

inherit "/std/spell";

void create() {
    ::create();
    set_spell_name("Heal");
    set_spell_school("abjuration");
    set_mana_cost(10);
    set_min_skill(0);
    set_spell_description("Channels healing energy to restore health.");
}

int do_spell(object caster, string args) {
    object target;
    object room;
    int power;
    int heal_amount;
    int old_hp;
    int new_hp;
    int max_hp;

    room = environment(caster);

    // Find target - default to self
    if (!args || args == "") {
        target = caster;
    } else if (args == "self" || args == "me") {
        target = caster;
    } else {
        // Find target by name in room
        if (room) {
            target = present(args, room);
        }
        if (!target) {
            tell_object(caster, "You don't see '" + args + "' here.\n");
            return 0;
        }
    }

    // Must be a living target
    if (!call_other(target, "is_living")) {
        tell_object(caster, "That's not a valid target for healing.\n");
        return 0;
    }

    // Calculate healing based on power
    power = calculate_power(caster);
    // Heal restores 50-100% of power as HP
    heal_amount = (power / 2) + random(power / 2);

    // Track HP change
    old_hp = call_other(target, "query_hp");
    max_hp = call_other(target, "query_max_hp");

    // Apply healing
    call_other(target, "heal", heal_amount);
    new_hp = call_other(target, "query_hp");

    int actual_heal;
    actual_heal = new_hp - old_hp;

    // Messages
    if (target == caster) {
        tell_object(caster, "You channel healing energy into yourself.\n");
        if (actual_heal > 0) {
            tell_object(caster, "You recover " + actual_heal + " health. (HP: " + new_hp + "/" + max_hp + ")\n");
        } else {
            tell_object(caster, "You are already at full health.\n");
        }
        if (room) {
            tell_room(room, capitalize(call_other(caster, "query_short")) +
                " glows with healing light.\n", ({ caster }));
        }
    } else {
        tell_object(caster, "You channel healing energy into " +
            call_other(target, "query_short") + ".\n");
        tell_object(target, capitalize(call_other(caster, "query_short")) +
            " channels healing energy into you.\n");
        if (actual_heal > 0) {
            tell_object(caster, "They recover " + actual_heal + " health.\n");
            tell_object(target, "You recover " + actual_heal + " health. (HP: " + new_hp + "/" + max_hp + ")\n");
        } else {
            tell_object(caster, "They are already at full health.\n");
        }
        if (room) {
            tell_room(room, capitalize(call_other(target, "query_short")) +
                " glows with healing light.\n", ({ caster, target }));
        }
    }

    return 1;
}
