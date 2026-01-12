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

    // Get description before unwielding
    string short_desc;
    short_desc = call_other(weapon, "query_short");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }

    if (call_other(player, "unwield_weapon")) {
        object room;
        room = environment(player);
        if (room) {
            call_other(room, "act", player,
                "You stop wielding " + short_desc + ".",
                "$N stops wielding " + short_desc + ".");
        } else {
            write("You stop wielding " + short_desc + ".");
        }
    } else {
        write("You can't do that.");
    }
}
