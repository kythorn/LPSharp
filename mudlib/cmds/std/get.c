// /cmds/std/get.c
// Pick up objects from the room or from containers

// Get an item from the room floor
void get_from_room(string item_name) {
    object player;
    object room;
    object target;
    string short_desc;

    player = this_player();
    room = environment(player);
    if (!room) {
        write("You are nowhere.");
        return;
    }

    // Find the object in the room
    target = present(item_name, room);
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

// Get an item from inside a container (corpse, bag, etc.)
void get_from_container(string item_name, string container_name) {
    object player;
    object room;
    object container;
    object target;
    string short_desc;
    string container_desc;

    player = this_player();
    room = environment(player);
    if (!room) {
        write("You are nowhere.");
        return;
    }

    // Find the container in the room or player's inventory
    container = present(container_name, room);
    if (!container) {
        container = present(container_name, player);
    }
    if (!container) {
        write("You don't see any " + container_name + " here.");
        return;
    }

    // Can't get things from living creatures
    if (call_other(container, "is_living")) {
        write("You can't take things from " + container_name + "!");
        return;
    }

    // Handle "all" - get everything from container
    if (item_name == "all") {
        object *contents;
        int i;
        int got_something;

        contents = all_inventory(container);
        if (sizeof(contents) == 0) {
            write("There's nothing in there.");
            return;
        }

        container_desc = call_other(container, "query_short");
        if (!container_desc) container_desc = "the container";

        got_something = 0;
        for (i = 0; i < sizeof(contents); i++) {
            short_desc = call_other(contents[i], "query_short");
            if (!short_desc || short_desc == "") {
                short_desc = "something";
            }
            move_object(contents[i], player);
            write("You get " + short_desc + " from " + container_desc + ".");
            got_something = 1;
        }

        if (got_something) {
            call_other(room, "act", player,
                "",
                "$N takes items from " + container_desc + ".");
        }
        return;
    }

    // Find the specific item in the container
    target = present(item_name, container);
    if (!target) {
        write("You don't see that in there.");
        return;
    }

    // Get descriptions
    short_desc = call_other(target, "query_short");
    if (!short_desc || short_desc == "") {
        short_desc = "something";
    }
    container_desc = call_other(container, "query_short");
    if (!container_desc || container_desc == "") {
        container_desc = "the container";
    }

    // Notify and move
    call_other(room, "act", player,
        "You get " + short_desc + " from " + container_desc + ".",
        "$N gets " + short_desc + " from " + container_desc + ".");

    move_object(target, player);
}

void main(string args) {
    object player;
    string item_name;
    string container_name;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    if (!args || args == "") {
        write("Get what?");
        return;
    }

    // Check for "get <item> from <container>" syntax
    if (sscanf(args, "%s from %s", item_name, container_name) == 2) {
        get_from_container(item_name, container_name);
        return;
    }

    // Standard "get <item>" from room
    get_from_room(args);
}
