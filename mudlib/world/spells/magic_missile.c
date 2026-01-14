// /world/spells/magic_missile.c
// Evocation spell - reliable low damage

inherit "/std/spell";

void create() {
    ::create();
    set_spell_name("Magic Missile");
    set_spell_school("evocation");
    set_mana_cost(5);
    set_min_skill(0);
    set_spell_description("Launches a bolt of magical force that never misses.");
}

int do_spell(object caster, string args) {
    object target;
    object room;
    int power;
    int damage;

    room = environment(caster);
    if (!room) {
        tell_object(caster, "You can't cast that here.\n");
        return 0;
    }

    // Find target
    if (!args || args == "") {
        // Target current combat opponent
        target = call_other(caster, "query_attacker");
        if (!target) {
            tell_object(caster, "Cast magic missile at whom?\n");
            return 0;
        }
    } else {
        // Find target by name in room
        target = present(args, room);
        if (!target) {
            tell_object(caster, "You don't see '" + args + "' here.\n");
            return 0;
        }
    }

    // Can't target self
    if (target == caster) {
        tell_object(caster, "You can't target yourself!\n");
        return 0;
    }

    // Must be a living target
    if (!call_other(target, "is_living")) {
        tell_object(caster, "That's not a valid target.\n");
        return 0;
    }

    // Calculate damage - magic missile always hits and does consistent damage
    power = calculate_power(caster);
    // Magic missile does 25-50% of power as damage (less than fireball but reliable)
    damage = (power / 4) + (power / 4);

    // Messages
    tell_object(caster, "You launch a bolt of magical force at " +
        call_other(target, "query_short") + "!\n");
    tell_object(target, capitalize(call_other(caster, "query_short")) +
        " launches a bolt of magical force at you!\n");
    tell_room(room, capitalize(call_other(caster, "query_short")) +
        " launches a bolt of magical force at " + call_other(target, "query_short") + "!\n",
        ({ caster, target }));

    // Deal damage (magic missile always hits, no miss chance)
    int actual_damage;
    actual_damage = call_other(target, "receive_damage", damage, caster);

    tell_object(caster, "The magic missile deals " + actual_damage + " damage!\n");

    // Start combat if not already fighting
    if (!call_other(caster, "query_in_combat")) {
        call_other(caster, "start_combat", target);
    }

    return 1;
}
