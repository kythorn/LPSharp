// /cmds/std/wield.c
// Wield a weapon from inventory

void main(string args) {
    object player;
    object weapon;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    if (!args || args == "") {
        write("Wield what?");
        return;
    }

    // Find weapon in inventory
    weapon = present(args, player);
    if (!weapon) {
        write("You don't have that.");
        return;
    }

    // Check if it's a weapon
    if (!call_other(weapon, "is_weapon")) {
        write("That's not a weapon.");
        return;
    }

    // Check if already wielding this weapon
    if (call_other(player, "query_wielded") == weapon) {
        write("You are already wielding that.");
        return;
    }

    // Wield the weapon (automatically unwields previous)
    if (call_other(player, "wield_weapon", weapon)) {
        string short_desc;
        short_desc = call_other(weapon, "query_short");
        if (!short_desc || short_desc == "") {
            short_desc = "something";
        }
        write("You wield " + short_desc + ".");

        // Notify room
        object room;
        object *others;
        int i;
        room = environment(player);
        if (room) {
            others = all_inventory(room);
            for (i = 0; i < sizeof(others); i++) {
                if (others[i] != player && call_other(others[i], "is_living")) {
                    tell_object(others[i], call_other(player, "query_name") + " wields " + short_desc + ".\n");
                }
            }
        }
    } else {
        write("You can't wield that.");
    }
}
