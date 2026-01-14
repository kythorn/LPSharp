// /world/rooms/forest/mushroom_grove.c
// A grove full of mushrooms - beetles

inherit "/std/room";

void create() {
    ::create();

    set_short("Mushroom Grove");
    set_long(
        "Enormous mushrooms of all colors grow in this damp corner of the forest. " +
        "Some tower overhead like strange umbrellas, while others cluster in fairy " +
        "rings on the forest floor. The air is thick with spores and the earthy smell " +
        "of decay. Large beetles crawl among the fungi, feeding on the decaying matter."
    );

    add_exit("east", "/world/rooms/forest/sunlit_path");
    add_exit("south", "/world/rooms/forest/dense_thicket");

    // Spawn beetles here
    add_spawn("/world/mobs/beetle");
    add_spawn("/world/mobs/beetle");
    enable_reset(120);
}
