// /cmds/std/drop.c
// Drop objects from inventory to the room

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
        write("Drop what?");
        return;
    }

    room = environment(player);
    if (!room) {
        write("You are nowhere.");
        return;
    }

    // Find the object in player's inventory
    target = present(args, player);
    if (!target) {
        write("You don't have that.");
        return;
    }

    // Check if item is wielded - must unwield first
    if (call_other(player, "query_wielded") == target) {
        write("You must unwield it first.");
        return;
    }

    // Check if item is worn armor - must remove first
    mapping worn;
    worn = call_other(player, "query_worn_armor");
    if (worn) {
        string *slots;
        int i;
        slots = keys(worn);
        for (i = 0; i < sizeof(slots); i++) {
            if (worn[slots[i]] == target) {
                write("You must remove it first.");
                return;
            }
        }
    }

    // Get the object's short description
    string short_desc;
    short_desc = call_other(target, "query_short");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }

    // Move object to room
    move_object(target, room);

    // Notify everyone
    call_other(room, "act", player,
        "You drop " + short_desc + ".",
        "$N drops " + short_desc + ".");
}
