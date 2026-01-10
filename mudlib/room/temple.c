// /room/temple.c
// The Temple of Light

inherit "/std/room";

void create() {
    ::create();

    set_short("Temple of Light");
    set_long(
        "You stand within the grand nave of the Temple of Light. Soaring stone columns " +
        "support a vaulted ceiling painted with scenes of celestial glory. Sunlight " +
        "streams through stained glass windows, casting rainbow patterns across the " +
        "polished marble floor. An ornate altar stands at the far end, draped in " +
        "white silk and adorned with golden candlesticks. The air is thick with the " +
        "sweet scent of incense."
    );

    add_exit("south", "/room/temple_road");
}
