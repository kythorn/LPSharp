// /world/rooms/wilderness/wolf_den.c
// Territory of a fierce wolf

inherit "/std/room";

void create() {
    ::create();

    set_short("Wolf Den");
    set_long(
        "The forest grows darker here, the trees pressing close together. A musky " +
        "animal scent hangs in the air, and you notice claw marks gouged into the " +
        "bark of nearby trees - territorial markings. Bones are scattered near a " +
        "shallow cave beneath a fallen oak. This is clearly the domain of a predator, " +
        "and you get the distinct feeling you are being watched."
    );

    add_exit("west", "/world/rooms/wilderness/spider_nest");
    add_exit("southwest", "/world/rooms/wilderness/deep_forest");

    // A fierce wolf guards this area
    add_spawn("/world/mobs/wolf");
    enable_reset(90);
}
