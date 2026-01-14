// Dragonslayer - Tier 6 Legendary weapon
// Dropped by the dragon boss

inherit "/std/weapon";

void create() {
    ::create();
    set_name("dragonslayer");
    add_id("sword");
    add_id("legendary sword");
    set_short("the legendary Dragonslayer sword");
    set_mass(18);
    set_damage(50);
    set_weapon_type("blade");
    set_skill_type("sword");
}
