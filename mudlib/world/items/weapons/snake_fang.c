// /world/items/weapons/snake_fang.c
// A venomous snake fang - intermediate weapon

inherit "/std/weapon";

void create() {
    ::create();
    set_short("a venomous snake fang");
    set_damage(5);
    set_weapon_type("piercing");
    set_mass(1);
}
