// /world/mobs/rabbit.c
// A harmless rabbit - very easy starter monster

inherit "/std/monster";

void create() {
    ::create();
    set_short("a rabbit");
    set_name("rabbit");

    // Very low stats - easiest monster
    set_str(1);
    set_dex(3);
    set_agi(4);
    set_con(1);

    // Very low HP
    set_max_hp(5);
    set_hp(5);

    // Non-aggressive - player initiates combat
    set_aggressive(0);
}
