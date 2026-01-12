// /cmds/std/get.c
// Pick up objects from the room

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
        write("Get what?");
        return;
    }

    room = environment(player);
    if (!room) {
        write("You are nowhere.");
        return;
    }

    // Find the object in the room
    target = present(args, room);
    if (!target) {
        write("You don't see that here.");
        return;
    }

    // Don't pick up living things
    if (call_other(target, "is_living")) {
        write("You can't pick that up!");
        return;
    }

    // Don't pick up yourself
    if (target == player) {
        write("You can't pick yourself up.");
        return;
    }

    // Move object to player inventory
    move_object(target, player);

    // Get the object's short description
    string short_desc;
    short_desc = call_other(target, "query_short");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }

    write("You pick up " + short_desc + ".");

    // Notify room
    object *others;
    int i;
    others = all_inventory(room);
    for (i = 0; i < sizeof(others); i++) {
        if (others[i] != player && call_other(others[i], "is_living")) {
            tell_object(others[i], call_other(player, "query_name") + " picks up " + short_desc + ".\n");
        }
    }
}
