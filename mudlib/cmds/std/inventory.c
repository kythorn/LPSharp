// /cmds/std/inventory.c
// Display player's inventory

void main(string args) {
    object player;
    object *items;
    object wielded;
    mapping worn;
    int i;
    int count;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    write("You are carrying:");

    // Get all items in inventory
    items = all_inventory(player);
    count = 0;

    // Get wielded weapon and worn armor to mark them
    wielded = call_other(player, "query_wielded");
    worn = call_other(player, "query_worn_armor");

    for (i = 0; i < sizeof(items); i++) {
        string short_desc;
        string suffix;

        short_desc = call_other(items[i], "query_short");
        if (!short_desc || short_desc == "") {
            short_desc = "something";
        }

        suffix = "";

        // Check if wielded
        if (items[i] == wielded) {
            suffix = " (wielded)";
        }

        // Check if worn
        if (worn) {
            string *slots;
            int j;
            slots = keys(worn);
            for (j = 0; j < sizeof(slots); j++) {
                if (worn[slots[j]] == items[i]) {
                    suffix = " (worn on " + slots[j] + ")";
                }
            }
        }

        write("  " + short_desc + suffix);
        count = count + 1;
    }

    if (count == 0) {
        write("  Nothing.");
    }
}
