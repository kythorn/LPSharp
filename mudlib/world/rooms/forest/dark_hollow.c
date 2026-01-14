// /world/rooms/forest/dark_hollow.c
// Darker area - wolves

inherit "/std/room";

void create() {
    ::create();

    set_short("Dark Hollow");
    set_long(
        "The forest grows noticeably darker here as the canopy thickens overhead.\n" +
        "Shadows pool in every corner, and an eerie silence has replaced the\n" +
        "cheerful birdsong of the lighter woods. Paw prints in the soft earth and\n" +
        "scattered bones suggest this is wolf territory. Yellow eyes seem to watch\n" +
        "from the darkness."
    );

    add_exit("north", "/world/rooms/forest/winding_trail");
    add_exit("south", "/world/rooms/forest/ancient_grove");

    // Spawn wolves here
    add_spawn("/world/mobs/wolf");
    add_spawn("/world/mobs/wolf");
    enable_reset(180);
}
