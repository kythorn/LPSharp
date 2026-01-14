// Fire Essence - Crafting material / trophy
// Dropped by fire elementals

inherit "/std/object";

void create() {
    ::create();

    set_short("a glowing fire essence");
    set_mass(1);
}

int id(string str) {
    if (str == "essence" || str == "fire essence" || str == "glowing essence") return 1;
    return ::id(str);
}
