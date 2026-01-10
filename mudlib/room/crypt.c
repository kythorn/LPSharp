// /room/crypt.c
// The ancient crypt

inherit "/std/room";

void create() {
    ::create();

    set_short("Ancient Crypt");
    set_long(
        "You stand in a vaulted chamber of ancient stone. Niches line the walls, each " +
        "containing the dusty remains of those interred here long ago. A massive stone " +
        "sarcophagus dominates the center of the room, its lid carved with the image " +
        "of a armored figure. Faint, phosphorescent fungi provide an eerie green light. " +
        "The silence here is oppressive, broken only by the occasional drip of water."
    );

    add_exit("up", "/room/crypt_entrance");
}
