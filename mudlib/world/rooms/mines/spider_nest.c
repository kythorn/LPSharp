// Mine Spider Nest

inherit "/std/room";

void create() {
    ::create();

    set_short("Spider-Infested Tunnel");
    set_long(
        "Thick webs cover every surface of this abandoned tunnel, turning " +
        "it into a nightmarish nest. The remains of unfortunate creatures " +
        "hang wrapped in silk from the ceiling. Giant cave spiders, far " +
        "larger than their surface cousins, lurk in the shadows. Their " +
        "many eyes gleam in what little light penetrates this deep."
    );

    add_exit("west", "/world/rooms/mines/upper_shaft");

    enable_reset(120);
    add_spawn("/world/mobs/cave_spider");
    add_spawn("/world/mobs/cave_spider");
}
