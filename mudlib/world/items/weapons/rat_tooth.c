// /world/items/weapons/rat_tooth.c
// A sharp rat tooth - starter weapon

inherit "/std/weapon";

void create() {
    ::create();
    set_short("a sharp rat tooth");
    set_damage(3);
    set_weapon_type("piercing");
    set_mass(1);
}
