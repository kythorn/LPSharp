// Goblin Blade - Tier 3 weapon
// Dropped by hobgoblins

inherit "/std/weapon";

void create() {
    ::create();
    set_name("blade");
    add_id("goblin blade");
    add_id("jagged blade");
    set_short("a jagged goblin blade");
    set_mass(12);
    set_damage(18);
    set_weapon_type("blade");
    set_skill_type("sword");
}
