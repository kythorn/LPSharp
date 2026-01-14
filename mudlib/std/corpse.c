// /std/corpse.c
// A corpse left behind when a player or monster dies
// Contains all inventory and equipped items from the deceased

inherit "/std/object";

string corpse_name;
int decay_time;

void create() {
    ::create();
    corpse_name = "someone";
    decay_time = 300;  // 5 minutes default (for monsters)
    set_short("a corpse");
}

void set_decay_time(int seconds) {
    decay_time = seconds;
}

void set_corpse_name(string name) {
    corpse_name = name;
    set_short("the corpse of " + name);
    set_long("This is the lifeless body of " + name + ". You could bury it.");
}

string query_corpse_name() {
    return corpse_name;
}

// Called when corpse is created - schedule decay
void start_decay() {
    call_out("decay", decay_time);
}

// Corpse decays - drop all contents and destruct
void decay() {
    object *contents;
    object env;
    int i;

    env = environment();

    // Only decay if we're in a room, not in someone's inventory
    if (env && !call_other(env, "is_room")) {
        // Reschedule decay check for later
        call_out("decay", 60);
        return;
    }

    contents = all_inventory(this_object());

    // Drop all contents to the room
    if (env) {
        for (i = 0; i < sizeof(contents); i++) {
            move_object(contents[i], env);
        }
        tell_room(env, "The corpse of " + corpse_name + " decays into dust.\n");
    }

    destruct(this_object());
}

// Allow searching/looting the corpse
int id(string str) {
    if (str == "corpse") return 1;
    if (str == "body") return 1;
    if (corpse_name != "" && str == lower_case(corpse_name) + " corpse") return 1;
    if (corpse_name != "" && str == "corpse of " + lower_case(corpse_name)) return 1;
    return 0;
}

// Add bury action when player enters room
void init() {
    ::init();
    add_action("do_bury", "bury");
}

int do_bury(string args) {
    object player;
    object env;
    object *contents;
    int i;

    // Check if they're trying to bury this corpse
    if (!args || args == "") {
        return 0;  // Let other objects handle "bury" with no args
    }

    if (!id(args)) {
        return 0;  // Not referring to this corpse
    }

    player = this_player();
    env = environment();

    if (!env || environment(player) != env) {
        write("You can't bury that from here.\n");
        return 1;
    }

    // Destroy all contents - they're buried with the corpse
    contents = all_inventory(this_object());
    for (i = 0; i < sizeof(contents); i++) {
        destruct(contents[i]);
    }

    // Announce the burial
    tell_room(env, call_other(player, "query_name") + " buries the corpse of " +
              corpse_name + ".\n", player);
    write("You dig a shallow grave and bury the remains of " + corpse_name + ".\n");

    destruct(this_object());
    return 1;
}
