// /world/rooms/town/temple_road_north.c
// Northern section of Temple Road

inherit "/std/room";

void create() {
    ::create();

    set_short("Temple Road North");
    set_long(
        "The road continues northward, the cobblestones becoming even more\n" +
        "pristine. Religious symbols are carved into the stone walls of nearby\n" +
        "buildings. The peaceful atmosphere of the temple district envelops you."
    );

    add_exit("south", "/world/rooms/town/temple_road");
    add_exit("north", "/world/rooms/town/temple_plaza");
    add_exit("east", "/world/guilds/healers");
    add_exit("west", "/world/rooms/town/healer");
}
