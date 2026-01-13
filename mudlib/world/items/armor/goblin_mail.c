// Goblin Mail - Tier 3 armor
// Dropped by hobgoblins

inherit "/std/armor";

void create() {
    ::create();

    set_short("crude goblin chainmail");
    set_mass(15);
    set_armor_class(4);
    set_slot("torso");
}

int id(string str) {
    if (str == "mail" || str == "chainmail" || str == "goblin mail" || str == "goblin chainmail") return 1;
    return ::id(str);
}
