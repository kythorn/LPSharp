// /std/player.c
// Base class for player objects

inherit "/std/living";

string player_name;
int xp;
int gold;
int level;

void create() {
    ::create();
    set_short("a player");
    player_name = "Guest";
    xp = 0;
    gold = 0;
    level = 1;
}

string query_name() {
    return player_name;
}

void set_name(string n) {
    player_name = n;
    set_short(n);
}

// XP and level getters/setters
int query_xp() { return xp; }
int query_gold() { return gold; }
int query_level() { return level; }

void set_xp(int val) { xp = val; }
void set_gold(int val) { gold = val; }
void set_level(int val) { level = val; }

void add_xp(int amount) {
    xp = xp + amount;
}

void add_gold(int amount) {
    gold = gold + amount;
    if (gold < 0) {
        gold = 0;
    }
}

// Save player data to file
// Returns 1 on success, 0 on failure
// Note: save_object saves all variables including stats, xp, gold
int save_player() {
    string path;

    if (player_name == "" || player_name == "Guest") {
        return 0;
    }

    path = "/secure/players/" + lower_case(player_name);
    return save_object(path);
}

// Restore player data from file
// Returns 1 on success, 0 if no save file exists
// Note: restore_object restores all variables including stats, xp, gold
int restore_player() {
    string path;

    if (player_name == "" || player_name == "Guest") {
        return 0;
    }

    path = "/secure/players/" + lower_case(player_name);
    return restore_object(path);
}

// Override die() for player death behavior
void die() {
    object death_room;
    object corpse;
    object death_location;
    object *inventory;
    mapping armor;
    string *slots;
    int i;

    // Remember where we died
    death_location = environment();

    // Stop combat
    in_combat = 0;
    attacker = 0;

    // Create a corpse at the death location
    if (death_location) {
        corpse = clone_object("/std/corpse");
        call_other(corpse, "set_corpse_name", player_name);

        // Unequip wielded weapon and move to corpse
        if (wielded_weapon) {
            move_object(wielded_weapon, corpse);
            wielded_weapon = 0;
        }

        // Unequip all worn armor and move to corpse
        armor = query_worn_armor();
        if (armor) {
            slots = keys(armor);
            for (i = 0; i < sizeof(slots); i++) {
                object piece;
                piece = armor[slots[i]];
                if (piece) {
                    move_object(piece, corpse);
                }
            }
            worn_armor = ([]);
        }

        // Move all inventory to the corpse
        inventory = all_inventory(this_object());
        for (i = 0; i < sizeof(inventory); i++) {
            move_object(inventory[i], corpse);
        }

        // Place corpse in the death location
        move_object(corpse, death_location);
        tell_room(death_location, player_name + " has died!\n");

        // Start corpse decay timer
        call_other(corpse, "start_decay");
    }

    // Message to player
    tell_object(this_object(),
        "You feel yourself slipping away...\n" +
        "Your vision fades to gray as you enter the netherworld.\n"
    );

    // Move to netherworld
    death_room = load_object("/world/rooms/special/netherworld");
    if (death_room) {
        move_object(this_object(), death_room);
    }

    // Restore HP to max (you're a spirit now)
    set_hp(query_max_hp());

    // Save the player state
    save_player();
}
