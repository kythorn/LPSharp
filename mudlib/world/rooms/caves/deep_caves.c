// Deep Caves - Tier 4 begins, hobgoblins appear

inherit "/std/room";

void create() {
    ::create();

    set_short("Deep Caves");
    set_long(
        "You descend into the deeper caves where the larger and more " +
        "dangerous hobgoblins make their domain. The ceilings are higher " +
        "here, necessary for the hulking creatures that patrol these " +
        "tunnels. Crude weapons and armor hang on the walls, evidence of " +
        "the hobgoblins' martial nature. A passage leads further down " +
        "into even darker depths."
    );

    add_exit("up", "/world/rooms/caves/warren");
    add_exit("down", "/world/rooms/mines/upper_shaft");

    enable_reset(120);
    add_spawn("/world/mobs/hobgoblin");
}
