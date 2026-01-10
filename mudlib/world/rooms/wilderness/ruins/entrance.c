// /world/rooms/wilderness/ruins/entrance.c
// Ancient ruins

inherit "/std/room";

void create() {
    ::create();

    set_short("Ancient Ruins");
    set_long(
        "Crumbling stone walls rise from the overgrown earth, the remains of what must " +
        "once have been a grand structure. Fallen columns lie scattered about, covered " +
        "in moss and creeping vines. Faded carvings on the remaining stones hint at a " +
        "civilization long forgotten. The wind whispers through the broken archways, " +
        "and you feel as though you're being watched by unseen eyes."
    );

    add_exit("east", "/world/rooms/wilderness/old_road");

    // Hidden entrance to underground area
    add_hidden_exit("down", "/world/rooms/wilderness/ruins/crypt_entrance");
}
