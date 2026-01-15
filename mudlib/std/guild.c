// /std/guild.c
// Base class for guilds that grant skills to members

inherit "/std/room";

string guild_name;
string *granted_skills;  // Skills this guild teaches
string *conflicting_guilds;  // Guilds that can't be joined with this one
string *taught_spells;  // Spell paths this guild can teach to members

void create() {
    ::create();
    guild_name = "Unknown Guild";
    granted_skills = ({});
    conflicting_guilds = ({});
    taught_spells = ({});
}

string query_guild_name() {
    return guild_name;
}

void set_guild_name(string name) {
    guild_name = name;
}

string *query_granted_skills() {
    return granted_skills;
}

void set_granted_skills(string *skills) {
    granted_skills = skills;
}

void add_granted_skill(string skill) {
    granted_skills = granted_skills + ({ skill });
}

string *query_conflicting_guilds() {
    return conflicting_guilds;
}

void set_conflicting_guilds(string *guilds) {
    conflicting_guilds = guilds;
}

void add_conflicting_guild(string guild_path) {
    conflicting_guilds = conflicting_guilds + ({ guild_path });
}

// Spell teaching functions
string *query_taught_spells() {
    return taught_spells;
}

void set_taught_spells(string *spells) {
    taught_spells = spells;
}

void add_taught_spell(string spell_path) {
    taught_spells = taught_spells + ({ spell_path });
}

// List spells available for a player to learn at this guild
// Shows spells they can learn based on their current skill level
void list_available_spells(object player) {
    int i;
    string guild_path;
    object spell;
    string spell_name;
    string spell_school;
    int learn_req;
    int player_skill;
    int can_learn;
    int already_knows;

    if (!player) return;

    guild_path = file_name(this_object());
    if (member(guild_path, "#") >= 0) {
        guild_path = explode(guild_path, "#")[0];
    }

    // Must be a member of this guild
    if (!call_other(player, "is_guild_member", guild_path)) {
        tell_object(player, "You must be a member of the " + guild_name + " to learn spells here.\n");
        return;
    }

    if (sizeof(taught_spells) == 0) {
        tell_object(player, "This guild does not teach any spells.\n");
        return;
    }

    tell_object(player, "=== Spells Available at " + guild_name + " ===\n");

    for (i = 0; i < sizeof(taught_spells); i++) {
        spell = load_object(taught_spells[i]);
        if (!spell) continue;

        spell_name = call_other(spell, "query_spell_name");
        spell_school = call_other(spell, "query_spell_school");
        learn_req = call_other(spell, "query_learn_skill");
        player_skill = call_other(player, "query_skill", spell_school);
        already_knows = call_other(player, "knows_spell", taught_spells[i]);

        if (already_knows) {
            tell_object(player, "  " + spell_name + " (" + spell_school + ") - [Already Known]\n");
        } else if (player_skill >= learn_req) {
            tell_object(player, "  " + spell_name + " (" + spell_school + " " + learn_req + ") - [Available]\n");
        } else {
            tell_object(player, "  " + spell_name + " (" + spell_school + " " + learn_req + ") - [Need " + spell_school + " " + learn_req + "]\n");
        }
    }
}

// Teach a spell to a player
// Returns 1 on success, 0 on failure
int teach_spell(object player, string spell_name_arg) {
    int i;
    string guild_path;
    object spell;
    string spell_name;
    string spell_school;
    string spell_path;
    int learn_req;
    int player_skill;

    if (!player) return 0;

    guild_path = file_name(this_object());
    if (member(guild_path, "#") >= 0) {
        guild_path = explode(guild_path, "#")[0];
    }

    // Must be a member of this guild
    if (!call_other(player, "is_guild_member", guild_path)) {
        tell_object(player, "You must be a member of the " + guild_name + " to learn spells here.\n");
        return 0;
    }

    if (sizeof(taught_spells) == 0) {
        tell_object(player, "This guild does not teach any spells.\n");
        return 0;
    }

    // Find the spell by name (case-insensitive)
    spell_path = 0;
    for (i = 0; i < sizeof(taught_spells); i++) {
        spell = load_object(taught_spells[i]);
        if (!spell) continue;

        spell_name = call_other(spell, "query_spell_name");
        if (lower_case(spell_name) == lower_case(spell_name_arg)) {
            spell_path = taught_spells[i];
            break;
        }
    }

    if (!spell_path) {
        tell_object(player, "This guild doesn't teach a spell called '" + spell_name_arg + "'.\n");
        return 0;
    }

    // Check if already knows
    if (call_other(player, "knows_spell", spell_path)) {
        tell_object(player, "You already know " + spell_name + ".\n");
        return 0;
    }

    // Check skill requirement
    spell_school = call_other(spell, "query_spell_school");
    learn_req = call_other(spell, "query_learn_skill");
    player_skill = call_other(player, "query_skill", spell_school);

    if (player_skill < learn_req) {
        tell_object(player, "You need " + spell_school + " skill of at least " + learn_req +
            " to learn " + spell_name + ". (You have " + player_skill + ")\n");
        return 0;
    }

    // Teach the spell
    call_other(player, "learn_spell", spell_path);
    tell_object(player, "You have learned " + spell_name + "!\n");
    return 1;
}

// Check if a player can join this guild
// Returns 1 if allowed, 0 if not
// Override in subclasses to add requirements
int can_join(object player) {
    int i;
    string *player_guilds;

    if (!player) return 0;

    // Check for conflicting guild memberships
    player_guilds = call_other(player, "query_guilds");
    for (i = 0; i < sizeof(conflicting_guilds); i++) {
        int j;
        for (j = 0; j < sizeof(player_guilds); j++) {
            if (player_guilds[j] == conflicting_guilds[i]) {
                return 0;  // Already in a conflicting guild
            }
        }
    }

    return 1;
}

// Called when a player joins this guild
// Grants all skills to the player
void on_join(object player) {
    int i;

    if (!player) return;

    for (i = 0; i < sizeof(granted_skills); i++) {
        call_other(player, "add_allowed_skill", granted_skills[i]);
    }

    tell_object(player, "You are now a member of the " + guild_name + "!\n");
    tell_object(player, "You can now train: " + implode(granted_skills, ", ") + "\n");
}

// Called when a player leaves this guild
// Removes granted skills (but doesn't remove skill points)
void on_leave(object player) {
    int i;

    if (!player) return;

    for (i = 0; i < sizeof(granted_skills); i++) {
        call_other(player, "remove_allowed_skill", granted_skills[i]);
    }

    tell_object(player, "You have left the " + guild_name + ".\n");
    tell_object(player, "You can no longer advance: " + implode(granted_skills, ", ") + "\n");
}

// Player command to join this guild
int do_join(object player) {
    string guild_path;

    if (!player) return 0;

    guild_path = file_name(this_object());
    // Remove clone number if present
    if (member(guild_path, "#") >= 0) {
        guild_path = explode(guild_path, "#")[0];
    }

    // Check if already a member
    if (call_other(player, "is_guild_member", guild_path)) {
        tell_object(player, "You are already a member of the " + guild_name + ".\n");
        return 0;
    }

    // Check if can join
    if (!can_join(player)) {
        tell_object(player, "You cannot join the " + guild_name + " at this time.\n");
        return 0;
    }

    // Add to player's guild list
    call_other(player, "add_guild", guild_path);

    // Grant skills
    on_join(player);

    return 1;
}

// Player command to leave this guild
int do_leave(object player) {
    string guild_path;

    if (!player) return 0;

    guild_path = file_name(this_object());
    if (member(guild_path, "#") >= 0) {
        guild_path = explode(guild_path, "#")[0];
    }

    // Check if a member
    if (!call_other(player, "is_guild_member", guild_path)) {
        tell_object(player, "You are not a member of the " + guild_name + ".\n");
        return 0;
    }

    // Remove skills
    on_leave(player);

    // Remove from player's guild list
    call_other(player, "remove_guild", guild_path);

    return 1;
}
