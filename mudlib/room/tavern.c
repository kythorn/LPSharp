// /room/tavern.c
// The Silver Flagon Tavern - a cozy establishment for travelers

inherit "/std/room";

void create() {
    ::create();

    set_short("The Silver Flagon Tavern");
    set_long(
        "The warm glow of a crackling fireplace welcomes you into this cozy tavern. " +
        "Heavy oak beams support the low ceiling, darkened by years of hearth smoke. " +
        "Rough wooden tables are scattered about, most occupied by locals nursing their ales. " +
        "A long bar runs along the northern wall, behind which shelves of bottles and " +
        "tankards gleam in the firelight. The smell of roasting meat and fresh bread " +
        "mingles with the pleasant aroma of pipe smoke."
    );

    add_exit("east", "/room/town_square");
    add_exit("up", "/room/tavern_rooms");
}
