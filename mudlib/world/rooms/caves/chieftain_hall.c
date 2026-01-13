// Chieftain's Hall - Boss of Goblin Caves

inherit "/std/room";

void create() {
    ::create();

    set_short("Chieftain's Hall");
    set_long(
        "This large cavern serves as the throne room for the goblin chief. " +
        "A crude throne made of bones and stolen treasure sits on a raised " +
        "platform at the far end. Torches burn in iron brackets, casting " +
        "dancing shadows on walls decorated with skulls and crude war " +
        "banners. This is clearly where the strongest goblins gather."
    );

    add_exit("south", "/world/rooms/caves/guard_post");

    enable_reset(180);
    add_spawn("/world/mobs/hobgoblin");
    add_spawn("/world/mobs/hobgoblin");
}
