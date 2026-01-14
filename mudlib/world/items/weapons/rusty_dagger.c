// Rusty Dagger - Tier 2 weapon
// Dropped by goblins

inherit "/std/weapon";

void create() {
    ::create();
    set_name("dagger");
    add_id("rusty dagger");
    set_short("a rusty dagger");
    set_mass(3);
    set_damage(8);
    set_weapon_type("piercing");
    set_skill_type("dagger");
}
