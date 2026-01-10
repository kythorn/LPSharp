// /world/rooms/wilderness/ruins/crypt_entrance.c
// Entrance to an underground crypt

inherit "/std/room";

void create() {
    ::create();

    set_short("Crypt Entrance");
    set_long(
        "Stone steps descend into darkness beneath the ruins. The air is cold and stale, " +
        "carrying the musty smell of ancient decay. Cobwebs stretch across the passage, " +
        "and strange scratching sounds echo from somewhere deeper within. Whatever lies " +
        "below has been undisturbed for centuries. Do you dare to explore further?"
    );

    add_exit("up", "/world/rooms/wilderness/ruins/entrance");
    add_exit("down", "/world/rooms/wilderness/ruins/crypt");
}
