// Troll Club - Tier 4 weapon
// Dropped by trolls

inherit "/std/weapon";

void create() {
    ::create();
    set_name("club");
    add_id("troll club");
    add_id("massive club");
    set_short("a massive troll club");
    set_mass(25);
    set_damage(28);
    set_weapon_type("blunt");
    set_skill_type("mace");
}
