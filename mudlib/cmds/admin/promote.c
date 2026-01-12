// promote.c - Change a user's access level
// Usage: promote <username> <level>
// Level can be: player, wizard, admin

void main(string args) {
    if (args == "" || args == 0) {
        write("Usage: promote <username> <level>");
        write("Levels: player, wizard, admin");
        write("Example: promote johndoe wizard");
        return;
    }

    // Parse arguments
    mixed *parts = explode(args, " ");

    if (sizeof(parts) < 2) {
        write("Usage: promote <username> <level>");
        write("Levels: player, wizard, admin");
        return;
    }

    string username = parts[0];
    string level = parts[1];

    // Validate level
    if (level != "player" && level != "wizard" && level != "admin") {
        write("Invalid level: " + level);
        write("Valid levels: player, wizard, admin");
        return;
    }

    // Check current level
    int current = query_access_level(username);
    if (current == 0) {
        write("Account not found: " + username);
        return;
    }

    string current_name;
    if (current == 1) current_name = "player";
    else if (current == 2) current_name = "wizard";
    else if (current == 3) current_name = "admin";
    else current_name = "unknown";

    // Set the new level
    int result = set_access_level(username, level);

    if (result) {
        write("Promoted " + username + " from " + current_name + " to " + level);

        // Note about home directory
        if (level == "wizard" || level == "admin") {
            write("Wizard home directory: /wizards/" + lower_case(username));
        }

        // Note about re-login
        write("Note: " + username + " will need to re-login for changes to take effect.");
    } else {
        write("Failed to promote " + username);
    }
}
