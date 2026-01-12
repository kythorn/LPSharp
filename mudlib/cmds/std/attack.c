// /cmds/std/attack.c
// Attack a target to start combat

void main(string args) {
    object player;
    object room;
    object target;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    if (!args || args == "") {
        write("Attack what?");
        return;
    }

    room = environment(player);
    if (!room) {
        write("You are nowhere.");
        return;
    }

    // Find target in room
    target = present(args, room);
    if (!target) {
        write("You don't see that here.");
        return;
    }

    // Can't attack yourself
    if (target == player) {
        write("You can't attack yourself.");
        return;
    }

    // Must be a living thing
    if (!call_other(target, "is_living")) {
        write("You can't attack that.");
        return;
    }

    // Check if already in combat with this target
    if (call_other(player, "query_in_combat") &&
        call_other(player, "query_attacker") == target) {
        write("You are already fighting " + call_other(target, "query_short") + "!");
        return;
    }

    // Start combat
    string target_name;
    target_name = call_other(target, "query_short");

    write("You attack " + target_name + "!");
    tell_object(target, call_other(player, "query_name") + " attacks you!\n");

    // Notify room
    object *others;
    int i;
    others = all_inventory(room);
    for (i = 0; i < sizeof(others); i++) {
        if (others[i] != player && others[i] != target) {
            if (call_other(others[i], "is_living")) {
                tell_object(others[i], call_other(player, "query_name") + " attacks " + target_name + "!\n");
            }
        }
    }

    call_other(player, "start_combat", target);
}
