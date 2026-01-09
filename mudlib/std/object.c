// /std/object.c
// Base class for all objects in the MUD

string short_desc;
int mass;

void create() {
    short_desc = "something";
    mass = 1;
}

string query_short() {
    return short_desc;
}

void set_short(string desc) {
    short_desc = desc;
}

int query_mass() {
    return mass;
}

void set_mass(int m) {
    mass = m;
}
