// /room/test.c
// Minimal test room

inherit "/std/room";

void create() {
    ::create();
    set_short("Test Room");
    set_long("A simple test room.");
}
