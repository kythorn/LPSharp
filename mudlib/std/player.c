// /std/player.c
// Base class for player objects

inherit "/std/living";

string player_name;
int xp;
int gold;
int level;
string *guilds;  // Array of guild paths this player belongs to
string cwd;      // Current working directory for wizards

void create() {
    ::create();
    set_short("a player");
    player_name = "Guest";
    xp = 0;
    gold = 0;
    level = 1;
    guilds = ({});
    cwd = "/";
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

// Current working directory functions
string query_cwd() { return cwd; }
void set_cwd(string path) { cwd = path; }

// Resolve a path relative to cwd
// Handles: absolute paths (/foo), relative paths (foo, ./foo), parent refs (..)
string resolve_path(string path) {
    string *parts;
    string *result;
    int i;

    if (!path || path == "") {
        return cwd;
    }

    // Absolute path - use as-is
    if (path[0] == '/') {
        parts = explode(path, "/");
    } else {
        // Relative path - combine with cwd
        parts = explode(cwd + "/" + path, "/");
    }

    // Process path components, handling . and ..
    result = ({});
    for (i = 0; i < sizeof(parts); i++) {
        if (parts[i] == "" || parts[i] == ".") {
            // Skip empty or current dir
            continue;
        } else if (parts[i] == "..") {
            // Go up one level
            if (sizeof(result) > 0) {
                result = result[0..sizeof(result)-2];
            }
        } else {
            result = result + ({ parts[i] });
        }
    }

    if (sizeof(result) == 0) {
        return "/";
    }

    return "/" + implode(result, "/");
}

void add_xp(int amount) {
    xp = xp + amount;
}

void add_gold(int amount) {
    gold = gold + amount;
    if (gold < 0) {
        gold = 0;
    }
}

// Guild membership functions
string *query_guilds() {
    return guilds;
}

int is_guild_member(string guild_path) {
    int i;
    for (i = 0; i < sizeof(guilds); i++) {
        if (guilds[i] == guild_path) {
            return 1;
        }
    }
    return 0;
}

void add_guild(string guild_path) {
    if (!is_guild_member(guild_path)) {
        guilds = guilds + ({ guild_path });
    }
}

void remove_guild(string guild_path) {
    int i;
    string *new_guilds;

    new_guilds = ({});
    for (i = 0; i < sizeof(guilds); i++) {
        if (guilds[i] != guild_path) {
            new_guilds = new_guilds + ({ guilds[i] });
        }
    }
    guilds = new_guilds;
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
