// Goblin Guard Post

inherit "/std/room";

void create() {
    ::create();

    set_short("Guard Post");
    set_long(
        "This wider section of cave has been turned into a guard post. " +
        "Makeshift barricades of bones and debris partially block the " +
        "passage. Empty bottles and gnawed bones show the guards' eating " +
        "habits. A larger passage leads north toward what sounds like " +
        "a much larger chamber."
    );

    add_exit("south", "/world/rooms/caves/tunnel");
    add_exit("north", "/world/rooms/caves/chieftain_hall");

    enable_reset(90);
    add_spawn("/world/mobs/goblin");
    add_spawn("/world/mobs/goblin");
}
