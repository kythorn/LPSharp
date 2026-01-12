// /world/rooms/wilderness/snake_pit.c
// A rocky area where snakes bask

inherit "/std/room";

void create() {
    ::create();

    set_short("Snake Pit");
    set_long(
        "Large flat rocks jut from the forest floor here, forming a natural clearing " +
        "where sunlight can reach the ground. The warmth has attracted reptilian " +
        "inhabitants - you can see the sinuous shapes of snakes basking on the sun-" +
        "warmed stones. Shed skins litter the ground, and you hear the occasional " +
        "warning rattle from the undergrowth."
    );

    add_exit("east", "/world/rooms/wilderness/spider_nest");
    add_exit("southeast", "/world/rooms/wilderness/deep_forest");

    // A venomous snake lives here
    add_spawn("/world/mobs/snake");
    enable_reset(90);
}
