// /world/items/weapons/snake_fang.c
// A venomous snake fang - early dagger weapon

inherit "/std/weapon";

void create() {
    ::create();
    set_name("fang");
    add_id("snake fang");
    add_id("snake's fang");
    set_short("a venomous snake fang");
    set_damage(4);
    set_weapon_type("piercing");
    set_skill_type("dagger");
    set_mass(1);
}
