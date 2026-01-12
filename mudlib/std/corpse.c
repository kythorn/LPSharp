// /std/corpse.c
// A corpse left behind when a player or monster dies
// Contains all inventory and equipped items from the deceased

inherit "/std/object";

string corpse_name;
int decay_time;

void create() {
    ::create();
    corpse_name = "someone";
    decay_time = 300;  // 5 minutes until corpse decays
    set_short("a corpse");
}

void set_corpse_name(string name) {
    corpse_name = name;
    set_short("the corpse of " + name);
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
