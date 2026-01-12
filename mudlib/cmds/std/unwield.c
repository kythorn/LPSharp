// /cmds/std/unwield.c
// Stop wielding current weapon

void main(string args) {
    object player;
    object weapon;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    weapon = call_other(player, "query_wielded");
    if (!weapon) {
        write("You aren't wielding anything.");
        return;
    }

    if (call_other(player, "unwield_weapon")) {
        string short_desc;
        short_desc = call_other(weapon, "query_short");
        if (!short_desc || short_desc == "") {
            short_desc = "something";
        }
        write("You stop wielding " + short_desc + ".");

        // Notify room
        object room;
        object *others;
        int i;
        room = environment(player);
        if (room) {
            others = all_inventory(room);
            for (i = 0; i < sizeof(others); i++) {
                if (others[i] != player && call_other(others[i], "is_living")) {
                    tell_object(others[i], call_other(player, "query_name") + " stops wielding " + short_desc + ".\n");
                }
            }
        }
    } else {
        write("You can't do that.");
    }
}
