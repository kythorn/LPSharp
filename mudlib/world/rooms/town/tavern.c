// /world/rooms/town/tavern.c
// The Silver Flagon Tavern - a cozy establishment for travelers
// Drinks cause intoxication which speeds up healing but impairs combat

inherit "/std/room";

void create() {
    ::create();

    set_short("The Silver Flagon Tavern");
    set_long(
        "The warm glow of a crackling fireplace welcomes you into this cozy tavern. " +
        "Heavy oak beams support the low ceiling, darkened by years of hearth smoke. " +
        "Rough wooden tables are scattered about, most occupied by locals nursing their ales. " +
        "A long bar runs along the northern wall, behind which shelves of bottles and " +
        "tankards gleam in the firelight. The smell of roasting meat and fresh bread " +
        "mingles with the pleasant aroma of pipe smoke.\n\n" +
        "Type MENU to see available drinks, or ORDER <drink> to buy one.\n" +
        "Drinking speeds healing but impairs combat ability!"
    );

    add_exit("east", "/world/rooms/town/square");
    add_exit("up", "/world/rooms/town/tavern_rooms");
}

void init() {
    ::init();
    add_action("do_order", "order");
    add_action("do_order", "buy");
    add_action("do_menu", "menu");
}

int do_menu(string arg) {
    write("=== The Silver Flagon Menu ===\n");
    write("  Ale   - A hearty brew        (mild buzz,  +1 regen)\n");
    write("  Mead  - Sweet honey wine     (good buzz,  +2 regen)\n");
    write("  Wine  - Fine vintage red     (strong,     +2 regen)\n");
    write("  Grog  - The strong stuff!    (very strong,+3 regen)\n");
    write("\n");
    write("ORDER <drink> to purchase. Drinks stack!\n");
    write("Warning: Being drunk impairs your combat ability.\n");
    return 1;
}

int do_order(string arg) {
    object player;
    int old_intox;
    int new_intox;
    string status;

    player = this_player();
    if (!player) {
        return 0;
    }

    if (!arg || arg == "") {
        write("The barkeeper asks: What would you like?\n");
        write("Type MENU to see our selection.\n");
        return 1;
    }

    arg = lower_case(arg);
    old_intox = call_other(player, "query_intoxication");

    if (arg == "ale") {
        new_intox = call_other(player, "add_intoxication", 15);
        write("The barkeeper slides a foaming mug of ale across the bar.\n");
        write("You drain it in one long gulp. The warmth spreads through you.\n");
        tell_room(this_object(), call_other(player, "query_name") + " downs a mug of ale.\n", player);
    } else if (arg == "mead") {
        new_intox = call_other(player, "add_intoxication", 20);
        write("The barkeeper pours you a glass of golden mead.\n");
        write("The sweet honey flavor goes down smoothly.\n");
        tell_room(this_object(), call_other(player, "query_name") + " enjoys some mead.\n", player);
    } else if (arg == "wine") {
        new_intox = call_other(player, "add_intoxication", 25);
        write("The barkeeper uncorks a dusty bottle and fills your glass.\n");
        write("A fine vintage! You feel it going to your head.\n");
        tell_room(this_object(), call_other(player, "query_name") + " savors a glass of wine.\n", player);
    } else if (arg == "grog") {
        new_intox = call_other(player, "add_intoxication", 35);
        write("The barkeeper eyes you warily, then pours a murky liquid.\n");
        write("You down it in one go. WOW! That's strong stuff!\n");
        tell_room(this_object(), call_other(player, "query_name") + " bravely drinks some grog!\n", player);
    } else {
        write("The barkeeper says: Sorry, we don't serve that here.\n");
        write("Type MENU to see what we have.\n");
        return 1;
    }

    // Show intoxication status
    status = call_other(player, "query_intoxication_status");
    write("You feel " + status + ".\n");

    // Warn about combat if getting too drunk
    if (new_intox >= 50 && old_intox < 50) {
        write("You're getting pretty drunk - combat will be difficult!\n");
    }

    // Show regen bonus
    int regen_bonus;
    regen_bonus = new_intox / 10;
    if (regen_bonus > 0) {
        write("Your wounds will heal faster while intoxicated. (+" + regen_bonus + " HP/tick)\n");
    }

    return 1;
}
