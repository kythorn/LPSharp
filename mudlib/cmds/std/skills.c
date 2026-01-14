// /cmds/std/skills.c
// Display player skills

int main(string args) {
    object player;
    string output;
    mapping skills;
    string *skill_names;
    string *allowed;
    string *basic;
    int i;
    int value;

    player = this_player();
    if (!player) {
        return 0;
    }

    output = "";
    output = output + "Skills for " + call_other(player, "query_name") + "\n";
    output = output + "========================================\n\n";

    skills = call_other(player, "query_skills");
    allowed = call_other(player, "query_allowed_skills");
    basic = call_other(player, "query_basic_skills");

    // If no skills learned yet
    if (!skills || sizeof(skills) == 0) {
        output = output + "You have not learned any skills yet.\n\n";
    } else {
        skill_names = keys(skills);

        // Display each skill with a progress bar
        // (sorting removed due to LPC string comparison limitations)
        for (i = 0; i < sizeof(skill_names); i++) {
            string skill_name;
            string bar;
            int pct;
            int bars;

            skill_name = skill_names[i];
            value = skills[skill_name];

            // Create a simple bar (20 chars wide, scale based on skill/100)
            pct = value;
            if (pct > 100) {
                pct = 100;
            }
            bars = pct / 5;  // 0-20 bars

            bar = "[";
            int k;
            for (k = 0; k < 20; k++) {
                if (k < bars) {
                    bar = bar + "=";
                } else {
                    bar = bar + " ";
                }
            }
            bar = bar + "]";

            // Pad skill name to 15 chars
            string padded_name;
            padded_name = skill_name;
            while (strlen(padded_name) < 15) {
                padded_name = padded_name + " ";
            }

            output = output + "  " + padded_name + " " + bar + " " + value + "\n";
        }
        output = output + "\n";
    }

    // Show allowed skills (from guilds)
    output = output + "Available Skills:\n";
    output = output + "  Basic: ";
    for (i = 0; i < sizeof(basic); i++) {
        if (i > 0) {
            output = output + ", ";
        }
        output = output + basic[i];
    }
    output = output + "\n";

    if (sizeof(allowed) > 0) {
        output = output + "  Guild: ";
        for (i = 0; i < sizeof(allowed); i++) {
            if (i > 0) {
                output = output + ", ";
            }
            output = output + allowed[i];
        }
        output = output + "\n";
    } else {
        output = output + "  Guild: (join a guild to unlock more skills)\n";
    }

    write(output);
    return 1;
}
