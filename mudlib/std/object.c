// /std/object.c
// Base class for all objects in the MUD

string short_desc;
int mass;

void create() {
    short_desc = "something";
    mass = 1;
}

// init() is called when this object enters an environment, or when
// something enters this object's environment. Override to add actions.
void init() {
    // Base implementation - nothing to do
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
