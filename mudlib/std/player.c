// /std/player.c
// Base class for player objects

inherit "/std/object";

string player_name;

void create() {
    ::create();
    set_short("a player");
    player_name = "Guest";
}

string query_name() {
    return player_name;
}

void set_name(string n) {
    player_name = n;
    set_short(n);
}

// Save player data to file
// Returns 1 on success, 0 on failure
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
int restore_player() {
    string path;

    if (player_name == "" || player_name == "Guest") {
        return 0;
    }

    path = "/secure/players/" + lower_case(player_name);
    return restore_object(path);
}
