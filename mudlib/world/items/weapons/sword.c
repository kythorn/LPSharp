// /obj/weapons/sword.c
// A generic sword weapon

inherit "/std/weapon";

void create() {
    ::create();
    set_name("sword");
    add_id("iron sword");
    set_short("an iron sword");
    set_damage(10);
    set_weapon_type("blade");
    set_skill_type("sword");
    set_mass(15);
}

int calculate_total_damage() {
    return query_damage() + query_mass();
}
