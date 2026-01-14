// /world/rooms/forest/ancient_grove.c
// Deepest part of forest - boss area with dire wolf

inherit "/std/room";

void create() {
    ::create();

    set_short("Ancient Grove");
    set_long(
        "You have reached the heart of Whisperwood Forest. Ancient trees tower overhead, " +
        "their trunks thick with moss and age. A strange stillness hangs in the air, " +
        "broken only by the occasional rustle of leaves. In the center of the grove, " +
        "a massive dire wolf guards its territory - larger and more fearsome than any " +
        "ordinary wolf. This creature is clearly the apex predator of this forest."
    );

    add_exit("north", "/world/rooms/forest/dark_hollow");
    add_exit("northwest", "/world/rooms/forest/stream_crossing");

    // Spawn the dire wolf boss
    add_spawn("/world/mobs/dire_wolf");
    enable_reset(300);  // Boss respawns slower
}
