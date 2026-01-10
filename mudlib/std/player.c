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
