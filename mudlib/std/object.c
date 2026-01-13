// /std/object.c
// Base class for all objects in the MUD

string short_desc;
int mass;

void create() {
    short_desc = "something";
    mass = 1;
}

// init() is called when this object enters an environment, or when
// something enters this object's environment. Override to add actions.
void init() {
    // Base implementation - nothing to do
}

string query_short() {
    return short_desc;
}

void set_short(string desc) {
    short_desc = desc;
}

int query_mass() {
    return mass;
}

void set_mass(int m) {
    mass = m;
}

// id() - Check if this object matches a given string
// Used by present() and other search functions
// Override in subclasses to add additional IDs
int id(string str) {
    if (!str || str == "") return 0;

    // Match against short description (case insensitive)
    if (short_desc && lower_case(str) == lower_case(short_desc)) {
        return 1;
    }

    // Check if the string is contained in the short description
    // This allows "goblin" to match "a snarling goblin"
    if (short_desc && member(lower_case(short_desc), lower_case(str)) >= 0) {
        return 1;
    }

    return 0;
}

// Message formatting helpers for consistent actor/observer messages
// Substitutes $N with actor name, $n with lowercase name, $P with possessive

string format_msg(string msg, object actor) {
    string name;
    string result;

    if (!actor) return msg;

    name = call_other(actor, "query_name");
    if (!name || name == "") {
        name = call_other(actor, "query_short");
    }
    if (!name || name == "") {
        name = "someone";
    }

    result = msg;

    // Replace $N with capitalized name
    result = replace_string(result, "$N", capitalize(name));

    // Replace $n with lowercase name
    result = replace_string(result, "$n", lower_case(name));

    // Replace $P with possessive (name's)
    result = replace_string(result, "$P", capitalize(name) + "'s");

    // Replace $p with lowercase possessive
    result = replace_string(result, "$p", lower_case(name) + "'s");

    return result;
}

// act() - Send formatted messages to actor and observers
//
// actor: The player/living doing the action
// actor_msg: Message the actor sees (e.g., "You drink the ale.")
// others_msg: Message others see, with $N for actor name
//             (e.g., "$N drinks the ale.")
// room: Optional room to message (defaults to actor's environment)
//
// Example:
//   act(player, "You drink a mug of ale.", "$N drinks a mug of ale.");
//
varargs void act(object actor, string actor_msg, string others_msg, object room) {
    object target_room;
    object *contents;
    int i;

    if (!actor) return;

    // Get the room to broadcast to
    if (room) {
        target_room = room;
    } else {
        target_room = environment(actor);
    }

    // Send message to actor
    if (actor_msg && actor_msg != "") {
        tell_object(actor, actor_msg + "\n");
    }

    // Send formatted message to others in room
    if (others_msg && others_msg != "" && target_room) {
        string formatted;
        formatted = format_msg(others_msg, actor);

        contents = all_inventory(target_room);
        for (i = 0; i < sizeof(contents); i++) {
            if (contents[i] != actor && call_other(contents[i], "is_living")) {
                tell_object(contents[i], formatted + "\n");
            }
        }
    }
}

// act_all() - Send same message to everyone including actor
// Useful for environmental messages
void act_all(string msg, object room) {
    object *contents;
    int i;

    if (!room || !msg || msg == "") return;

    contents = all_inventory(room);
    for (i = 0; i < sizeof(contents); i++) {
        if (call_other(contents[i], "is_living")) {
            tell_object(contents[i], msg + "\n");
        }
    }
}
