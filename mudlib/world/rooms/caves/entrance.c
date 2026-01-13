// Goblin Cave Entrance - Tier 3 area begins
// Connects from wilderness/old_road

inherit "/std/room";

void create() {
    ::create();

    set_short("Cave Entrance");
    set_long(
        "A dark cave mouth yawns before you, carved into the rocky hillside. " +
        "Crude torches flicker just inside the entrance, and the stench of " +
        "goblin fills the air. Scratching sounds and guttural voices echo " +
        "from within. Piles of bones and refuse litter the ground near the " +
        "entrance - clearly, something dangerous lives here."
    );

    add_exit("west", "/world/rooms/wilderness/old_road");
    add_exit("east", "/world/rooms/caves/tunnel");

    add_spawn("/world/mobs/goblin");
    enable_reset(90);
}
