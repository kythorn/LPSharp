// /std/room.c
// Base class for all rooms in the MUD
// Provides exit management, descriptions, and room-based messaging

inherit "/std/object";

// Room descriptions
string long_desc;

// Exits - each direction stores a destination path or empty string
string exit_north;
string exit_south;
string exit_east;
string exit_west;
string exit_northeast;
string exit_northwest;
string exit_southeast;
string exit_southwest;
string exit_up;
string exit_down;

// Hidden exits - stores the hidden direction (simple for now)
string hidden_exit_dir;

void create() {
    ::create();
    set_short("A room");
    long_desc = "You are in a nondescript room.";

    // Initialize all exits to empty
    exit_north = "";
    exit_south = "";
    exit_east = "";
    exit_west = "";
    exit_northeast = "";
    exit_northwest = "";
    exit_southeast = "";
    exit_southwest = "";
    exit_up = "";
    exit_down = "";

    hidden_exit_dir = "";
}

// Set the long description
void set_long(string desc) {
    long_desc = desc;
}

string query_long() {
    return long_desc;
}

// Add an exit in a direction
void add_exit(string direction, string destination) {
    if (direction == "n") { exit_north = destination; return; }
    if (direction == "north") { exit_north = destination; return; }
    if (direction == "s") { exit_south = destination; return; }
    if (direction == "south") { exit_south = destination; return; }
    if (direction == "e") { exit_east = destination; return; }
    if (direction == "east") { exit_east = destination; return; }
    if (direction == "w") { exit_west = destination; return; }
    if (direction == "west") { exit_west = destination; return; }
    if (direction == "ne") { exit_northeast = destination; return; }
    if (direction == "northeast") { exit_northeast = destination; return; }
    if (direction == "nw") { exit_northwest = destination; return; }
    if (direction == "northwest") { exit_northwest = destination; return; }
    if (direction == "se") { exit_southeast = destination; return; }
    if (direction == "southeast") { exit_southeast = destination; return; }
    if (direction == "sw") { exit_southwest = destination; return; }
    if (direction == "southwest") { exit_southwest = destination; return; }
    if (direction == "u") { exit_up = destination; return; }
    if (direction == "up") { exit_up = destination; return; }
    if (direction == "d") { exit_down = destination; return; }
    if (direction == "down") { exit_down = destination; return; }
}

// Add a hidden exit
void add_hidden_exit(string direction, string destination) {
    add_exit(direction, destination);
    hidden_exit_dir = direction;
}

// Query an exit destination
string query_exit(string direction) {
    if (direction == "n") { return exit_north; }
    if (direction == "north") { return exit_north; }
    if (direction == "s") { return exit_south; }
    if (direction == "south") { return exit_south; }
    if (direction == "e") { return exit_east; }
    if (direction == "east") { return exit_east; }
    if (direction == "w") { return exit_west; }
    if (direction == "west") { return exit_west; }
    if (direction == "ne") { return exit_northeast; }
    if (direction == "northeast") { return exit_northeast; }
    if (direction == "nw") { return exit_northwest; }
    if (direction == "northwest") { return exit_northwest; }
    if (direction == "se") { return exit_southeast; }
    if (direction == "southeast") { return exit_southeast; }
    if (direction == "sw") { return exit_southwest; }
    if (direction == "southwest") { return exit_southwest; }
    if (direction == "u") { return exit_up; }
    if (direction == "up") { return exit_up; }
    if (direction == "d") { return exit_down; }
    if (direction == "down") { return exit_down; }
    return "";
}

// Get the obvious exits line for display
string query_exits() {
    string result;
    int count;

    result = "";
    count = 0;

    if (exit_north != "") {
        if (hidden_exit_dir != "north") {
            if (count > 0) { result = result + ", "; }
            result = result + "north";
            count = count + 1;
        }
    }
    if (exit_south != "") {
        if (hidden_exit_dir != "south") {
            if (count > 0) { result = result + ", "; }
            result = result + "south";
            count = count + 1;
        }
    }
    if (exit_east != "") {
        if (hidden_exit_dir != "east") {
            if (count > 0) { result = result + ", "; }
            result = result + "east";
            count = count + 1;
        }
    }
    if (exit_west != "") {
        if (hidden_exit_dir != "west") {
            if (count > 0) { result = result + ", "; }
            result = result + "west";
            count = count + 1;
        }
    }
    if (exit_northeast != "") {
        if (hidden_exit_dir != "northeast") {
            if (count > 0) { result = result + ", "; }
            result = result + "northeast";
            count = count + 1;
        }
    }
    if (exit_northwest != "") {
        if (hidden_exit_dir != "northwest") {
            if (count > 0) { result = result + ", "; }
            result = result + "northwest";
            count = count + 1;
        }
    }
    if (exit_southeast != "") {
        if (hidden_exit_dir != "southeast") {
            if (count > 0) { result = result + ", "; }
            result = result + "southeast";
            count = count + 1;
        }
    }
    if (exit_southwest != "") {
        if (hidden_exit_dir != "southwest") {
            if (count > 0) { result = result + ", "; }
            result = result + "southwest";
            count = count + 1;
        }
    }
    if (exit_up != "") {
        if (hidden_exit_dir != "up") {
            if (count > 0) { result = result + ", "; }
            result = result + "up";
            count = count + 1;
        }
    }
    if (exit_down != "") {
        if (hidden_exit_dir != "down") {
            if (count > 0) { result = result + ", "; }
            result = result + "down";
            count = count + 1;
        }
    }

    if (count == 0) {
        return "There are no obvious exits.";
    }

    return "Obvious exits: " + result;
}
