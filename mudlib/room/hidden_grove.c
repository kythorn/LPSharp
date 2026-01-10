// /room/hidden_grove.c
// A secret grove in the forest

inherit "/std/room";

void create() {
    ::create();

    set_short("Hidden Grove");
    set_long(
        "You have discovered a hidden grove, a perfect circle of ancient standing stones " +
        "rising from a carpet of soft grass. Wildflowers bloom in impossible colors, " +
        "and the air shimmers with a faint, ethereal light. A small spring bubbles up " +
        "at the center, its water crystal clear and somehow glowing faintly. This place " +
        "feels ancient and magical, untouched by the outside world."
    );

    add_exit("east", "/room/forest_path");
}
