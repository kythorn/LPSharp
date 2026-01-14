// /cmds/std/cast.c
// Cast a spell

int main(string args) {
    object player;
    object spell;
    string *known_spells;
    string spell_args;
    int i;

    player = this_player();
    if (!player) {
        return 0;
    }

    if (!args || args == "") {
        write("Usage: cast <spell name> [target]\n");
        write("Type 'spells' to see your known spells.\n");
        return 1;
    }

    // Get player's known spells
    known_spells = call_other(player, "query_known_spells");
    if (!known_spells || sizeof(known_spells) == 0) {
        write("You don't know any spells.\n");
        write("Join a mage guild to learn magic!\n");
        return 1;
    }

    // Search through known spells to find one that matches
    object best_match;
    string best_match_name;
    string best_match_args;
    int best_match_len;

    best_match = 0;
    best_match_name = "";
    best_match_args = "";
    best_match_len = 0;

    string lower_args;
    lower_args = lower_case(args);

    for (i = 0; i < sizeof(known_spells); i++) {
        object test_spell;
        string test_name;
        int name_len;

        test_spell = load_object(known_spells[i]);
        if (!test_spell) {
            continue;
        }

        test_name = lower_case(call_other(test_spell, "query_spell_name"));
        name_len = strlen(test_name);

        // Check if args matches exactly
        if (lower_args == test_name) {
            if (name_len > best_match_len) {
                best_match = test_spell;
                best_match_name = test_name;
                best_match_args = "";
                best_match_len = name_len;
            }
        } else {
            // Check if args starts with spell name followed by space
            // Use sscanf to try to match
            string remaining;
            if (sscanf(lower_args, test_name + " %s", remaining) == 1) {
                if (name_len > best_match_len) {
                    best_match = test_spell;
                    best_match_name = test_name;
                    // Get original case for remaining args
                    // Use sscanf on original args
                    sscanf(args, test_name + " %s", remaining);
                    // Try with capitalized version too
                    if (!remaining || remaining == "") {
                        string cap_name;
                        cap_name = capitalize(test_name);
                        sscanf(args, cap_name + " %s", remaining);
                    }
                    if (!remaining) remaining = "";
                    best_match_args = remaining;
                    best_match_len = name_len;
                }
            }
        }
    }

    if (!best_match) {
        write("You don't know a spell called '" + args + "'.\n");
        write("Type 'spells' to see your known spells.\n");
        return 1;
    }

    // Cast the spell
    call_other(best_match, "cast", player, best_match_args);

    return 1;
}
