// /world/rooms/wilderness/spider_nest.c
// A dark area infested with spiders

inherit "/std/room";

void create() {
    ::create();

    set_short("Spider Nest");
    set_long(
        "Thick webs stretch between the trees here, glistening with morning dew. The " +
        "silken strands form intricate patterns that catch what little light filters " +
        "through the dense canopy. Wrapped bundles hang from branches - the remains of " +
        "unfortunate prey. The air feels thick and still, and you sense movement in " +
        "the shadows above."
    );

    add_exit("north", "/world/rooms/wilderness/forest_path");
    add_exit("west", "/world/rooms/wilderness/snake_pit");
    add_exit("east", "/world/rooms/wilderness/wolf_den");

    // A large spider lurks here
    add_spawn("/world/mobs/spider");
    enable_reset(60);
}
