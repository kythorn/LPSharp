// /world/items/weapons/wolf_fang.c
// A massive fang from the dire wolf boss - best weapon in the forest

inherit "/std/weapon";

void create() {
    ::create();
    set_name("fang");
    add_id("wolf fang");
    add_id("dire wolf fang");
    set_short("a dire wolf fang");
    set_long(
        "This massive fang once belonged to a dire wolf, the apex predator of " +
        "Whisperwood Forest. It's as long as a dagger and wickedly sharp."
    );
    set_damage(8);
    set_weapon_type("piercing");
    set_skill_type("dagger");
    set_mass(2);
}
