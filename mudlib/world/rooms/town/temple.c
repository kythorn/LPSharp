// /world/rooms/town/temple.c
// The Temple of Light

inherit "/std/room";

void create() {
    ::create();

    set_short("Temple of Light");
    set_long(
        "You stand within the grand nave of the Temple of Light. Soaring stone\n" +
        "columns support a vaulted ceiling painted with scenes of celestial glory.\n" +
        "Sunlight streams through stained glass windows, casting rainbow patterns\n" +
        "across the polished marble floor. An ornate altar stands at the far end,\n" +
        "draped in white silk and adorned with golden candlesticks. The air is\n" +
        "thick with the sweet scent of incense."
    );

    add_exit("east", "/world/rooms/town/temple_plaza");
}
