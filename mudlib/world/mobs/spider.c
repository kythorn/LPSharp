// /world/mobs/spider.c
// A large forest spider - drops web gloves

inherit "/std/monster";

void create() {
    ::create();
    set_short("a large spider");
    set_name("spider");

    // Slightly tougher than rat
    set_str(2);
    set_dex(3);
    set_agi(2);
    set_con(2);

    set_max_hp(8);
    set_hp(8);

    // Non-aggressive - player initiates
    set_aggressive(0);

    set_xp_value(10);

    // Drop web gloves
    add_drop("/world/items/armor/web_gloves");
}
