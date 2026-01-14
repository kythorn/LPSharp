// /std/player.c
// Base class for player objects

inherit "/std/living";

string player_name;
int xp;
int gold;
int level;
string *guilds;  // Array of guild paths this player belongs to
string cwd;      // Current working directory for wizards

// Saved equipment paths (for persistence across logins)
string saved_weapon_path;
mapping saved_armor_paths;  // slot -> path

void create() {
    ::create();
    set_short("a player");
    player_name = "Guest";
    xp = 0;
    gold = 0;
    level = 1;
    guilds = ({});
    cwd = "/";
    saved_weapon_path = "";
    saved_armor_paths = ([]);
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
    mapping armor;
    string *slots;
    int i;

    if (player_name == "" || player_name == "Guest") {
        return 0;
    }

    // Capture current equipment paths before saving
    if (wielded_weapon && objectp(wielded_weapon)) {
        saved_weapon_path = file_name(wielded_weapon);
        // Remove clone suffix if present (e.g., "/world/items/sword#1" -> "/world/items/sword")
        if (member(saved_weapon_path, "#") != -1) {
            saved_weapon_path = explode(saved_weapon_path, "#")[0];
        }
    } else {
        saved_weapon_path = "";
    }

    // Capture worn armor paths
    saved_armor_paths = ([]);
    armor = query_worn_armor();
    if (armor) {
        slots = keys(armor);
        for (i = 0; i < sizeof(slots); i++) {
            object piece;
            string piece_path;
            piece = armor[slots[i]];
            if (piece && objectp(piece)) {
                piece_path = file_name(piece);
                if (member(piece_path, "#") != -1) {
                    piece_path = explode(piece_path, "#")[0];
                }
                saved_armor_paths[slots[i]] = piece_path;
            }
        }
    }

    path = "/secure/players/" + lower_case(player_name);
    return save_object(path);
}

// Restore player data from file
// Returns 1 on success, 0 if no save file exists
// Note: restore_object restores all variables including stats, xp, gold
int restore_player() {
    string path;
    int result;
    string *slots;
    int i;

    if (player_name == "" || player_name == "Guest") {
        return 0;
    }

    path = "/secure/players/" + lower_case(player_name);
    result = restore_object(path);

    // Clear live equipment references first
    wielded_weapon = 0;
    worn_armor = ([]);

    // Recreate saved equipment
    if (saved_weapon_path && saved_weapon_path != "") {
        object weapon;
        weapon = clone_object(saved_weapon_path);
        if (weapon) {
            move_object(weapon, this_object());
            wield_weapon(weapon);
        }
    }

    // Recreate saved armor
    if (saved_armor_paths) {
        slots = keys(saved_armor_paths);
        for (i = 0; i < sizeof(slots); i++) {
            string armor_path;
            object armor;
            armor_path = saved_armor_paths[slots[i]];
            if (armor_path && armor_path != "") {
                armor = clone_object(armor_path);
                if (armor) {
                    move_object(armor, this_object());
                    wear_armor(armor);
                }
            }
        }
    }

    return result;
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
        call_other(corpse, "set_decay_time", 3600);  // Player corpses last 1 hour

        // Unequip wielded weapon and move to corpse
        if (wielded_weapon && objectp(wielded_weapon)) {
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
                if (piece && objectp(piece)) {
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

    // Clear saved equipment so we don't respawn with gear
    saved_weapon_path = "";
    saved_armor_paths = ([]);

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
