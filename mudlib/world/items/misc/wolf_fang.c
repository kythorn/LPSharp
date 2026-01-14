// /world/items/misc/wolf_fang.c
// A trophy from the dire wolf

inherit "/std/object";

void create() {
    ::create();
    set_short("a dire wolf fang");
    set_long(
        "This massive fang once belonged to a dire wolf, the apex predator of " +
        "Whisperwood Forest. It's as long as a dagger and wickedly sharp. " +
        "A trophy worthy of any adventurer."
    );
    set_mass(1);
}
