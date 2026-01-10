// /obj/weapons/sword.c
// A generic sword weapon

inherit "/std/weapon";

void create() {
    ::create();
    set_short("a iron sword");
    set_damage(10);
    set_weapon_type("blade");
    set_mass(15);
}

int calculate_total_damage() {
    return query_damage() + query_mass();
}
