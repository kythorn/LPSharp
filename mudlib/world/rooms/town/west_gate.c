// /world/rooms/town/west_gate.c
// The West Gate of town

inherit "/std/room";

void create() {
    ::create();

    set_short("West Gate");
    set_long(
        "The West Gate opens onto the trade road leading to distant cities.\n" +
        "Merchant caravans gather here, preparing for the long journey west.\n" +
        "Guards inspect cargo and collect duties from traders. The road beyond\n" +
        "stretches toward the horizon."
    );

    add_exit("east", "/world/rooms/town/west_road_3");
    // No exit west - leads to trade routes not yet implemented
}
