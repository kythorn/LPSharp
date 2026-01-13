// Flooded Shaft - Leads to Dragon's Lair

inherit "/std/room";

void create() {
    ::create();

    set_short("Flooded Shaft");
    set_long(
        "The tunnel descends to a partially flooded area where dark water " +
        "has pooled in the lowest sections. A wooden platform crosses " +
        "above the water, creaking ominously with each step. Strange heat " +
        "rises from somewhere below, warming the water and filling the " +
        "air with steam. A passage leads downward into an eerie red glow."
    );

    add_exit("north", "/world/rooms/mines/main_tunnel");
    add_exit("down", "/world/rooms/dragon/approach");

    enable_reset(120);
    add_spawn("/world/mobs/cave_spider");
}
