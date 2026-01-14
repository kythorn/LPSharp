// /world/spells/fireball.c
// Evocation spell - direct fire damage

inherit "/std/spell";

void create() {
    ::create();
    set_spell_name("Fireball");
    set_spell_school("evocation");
    set_mana_cost(15);
    set_min_skill(10);
    set_spell_description("Hurls a ball of fire at your target, dealing significant damage.");
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
            tell_object(caster, "Cast fireball at whom?\n");
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
        tell_object(caster, "You can't fireball yourself!\n");
        return 0;
    }

    // Must be a living target
    if (!call_other(target, "is_living")) {
        tell_object(caster, "That's not a valid target.\n");
        return 0;
    }

    // Calculate damage based on power
    power = calculate_power(caster);
    // Fireball does 50-100% of power as damage
    damage = (power / 2) + random(power / 2);

    // Messages
    tell_object(caster, "You hurl a ball of fire at " +
        call_other(target, "query_short") + "!\n");
    tell_object(target, capitalize(call_other(caster, "query_short")) +
        " hurls a ball of fire at you!\n");
    tell_room(room, capitalize(call_other(caster, "query_short")) +
        " hurls a ball of fire at " + call_other(target, "query_short") + "!\n",
        ({ caster, target }));

    // Deal damage
    int actual_damage;
    actual_damage = call_other(target, "receive_damage", damage, caster);

    tell_object(caster, "The fireball deals " + actual_damage + " damage!\n");

    // Start combat if not already fighting
    if (!call_other(caster, "query_in_combat")) {
        call_other(caster, "start_combat", target);
    }

    return 1;
}
