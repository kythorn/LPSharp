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

    // Get the object's short description before moving
    string short_desc;
    short_desc = call_other(target, "query_short");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }

    // Notify everyone (do this before moving so room message works)
    call_other(room, "act", player,
        "You pick up " + short_desc + ".",
        "$N picks up " + short_desc + ".");

    // Move object to player inventory
    move_object(target, player);
}
