// /world/rooms/forest/old_oak.c
// Ancient oak tree - spiders

inherit "/std/room";

void create() {
    ::create();

    set_short("Old Oak");
    set_long(
        "A massive oak tree dominates this area, its gnarled trunk wider than three " +
        "men standing shoulder to shoulder. Thick webs stretch between its lower " +
        "branches, glistening with morning dew. The silken strands suggest that large " +
        "spiders have made their home here. Bones of small animals litter the ground."
    );

    add_exit("north", "/world/rooms/forest/mossy_clearing");
    add_exit("west", "/world/rooms/forest/winding_trail");

    // Spawn spiders here
    add_spawn("/world/mobs/spider");
    add_spawn("/world/mobs/spider");
    enable_reset(120);
}
