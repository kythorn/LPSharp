// /cmds/std/remove.c
// Remove worn armor

void main(string args) {
    object player;
    object armor;
    mapping worn;
    string *slots;
    int i;
    int found;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    if (!args || args == "") {
        write("Remove what?");
        return;
    }

    // First try to find it as an object in inventory
    armor = present(args, player);

    // Check if it's worn
    worn = call_other(player, "query_worn_armor");
    found = 0;

    if (armor && worn) {
        slots = keys(worn);
        for (i = 0; i < sizeof(slots); i++) {
            if (worn[slots[i]] == armor) {
                found = 1;
                break;
            }
        }
    }

    if (!found) {
        // Maybe they typed the slot name
        if (worn && worn[args]) {
            armor = worn[args];
            found = 1;
        }
    }

    if (!found) {
        write("You aren't wearing that.");
        return;
    }

    // Get description before removing
    string short_desc;
    string slot;
    short_desc = call_other(armor, "query_short");
    slot = call_other(armor, "query_slot");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }

    // Remove the armor
    if (call_other(player, "remove_armor_obj", armor)) {
        object room;
        room = environment(player);
        if (room) {
            call_other(room, "act", player,
                "You remove " + short_desc + " from your " + slot + ".",
                "$N removes " + short_desc + ".");
        } else {
            write("You remove " + short_desc + " from your " + slot + ".");
        }
    } else {
        write("You can't remove that.");
    }
}
