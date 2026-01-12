// /cmds/std/wear.c
// Wear armor from inventory

void main(string args) {
    object player;
    object armor;
    string slot;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    if (!args || args == "") {
        write("Wear what?");
        return;
    }

    // Find armor in inventory
    armor = present(args, player);
    if (!armor) {
        write("You don't have that.");
        return;
    }

    // Check if it's armor
    if (!call_other(armor, "is_armor")) {
        write("You can't wear that.");
        return;
    }

    // Get the slot
    slot = call_other(armor, "query_slot");
    if (!slot || slot == "") {
        write("That armor has no slot defined.");
        return;
    }

    // Check if already wearing something in that slot
    mapping worn;
    worn = call_other(player, "query_worn_armor");
    if (worn && worn[slot]) {
        write("You are already wearing something on your " + slot + ".");
        return;
    }

    // Wear the armor
    if (call_other(player, "wear_armor", armor)) {
        string short_desc;
        object room;

        short_desc = call_other(armor, "query_short");
        if (!short_desc || short_desc == "") {
            short_desc = "something";
        }

        room = environment(player);
        if (room) {
            call_other(room, "act", player,
                "You wear " + short_desc + " on your " + slot + ".",
                "$N wears " + short_desc + ".");
        } else {
            write("You wear " + short_desc + " on your " + slot + ".");
        }
    } else {
        write("You can't wear that.");
    }
}
