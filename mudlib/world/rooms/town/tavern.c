// /world/rooms/town/tavern.c
// The Silver Flagon Tavern - a cozy establishment for travelers

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
        "You can ORDER drinks here."
    );

    add_exit("east", "/world/rooms/town/square");
    add_exit("up", "/world/rooms/town/tavern_rooms");
}

void init() {
    ::init();
    add_action("do_order", "order");
}

int do_order(string arg) {
    if (!arg || arg == "") {
        write("The barkeeper asks: What would you like? We have ale, mead, and wine.\n");
        return 1;
    }

    if (arg == "ale") {
        write("The barkeeper slides a foaming mug of ale across the bar.\n");
        write("You take a long sip. Refreshing!\n");
        tell_room(this_object(), this_player()->query_short() + " orders an ale.\n", this_player());
        return 1;
    }

    if (arg == "mead") {
        write("The barkeeper pours you a glass of golden mead.\n");
        write("The sweet honey flavor warms your throat.\n");
        tell_room(this_object(), this_player()->query_short() + " orders some mead.\n", this_player());
        return 1;
    }

    if (arg == "wine") {
        write("The barkeeper uncorks a dusty bottle and fills your glass.\n");
        write("A fine vintage indeed.\n");
        tell_room(this_object(), this_player()->query_short() + " orders wine.\n", this_player());
        return 1;
    }

    notify_fail("The barkeeper says: Sorry, we don't serve that here.\n");
    return 0;
}
