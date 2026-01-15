// /world/guilds/healers.c
// The Healers Guild - grants abjuration and divination magic

inherit "/std/guild";

void create() {
    ::create();

    set_guild_name("Healers Guild");

    set_short("Healers Guild Temple");
    set_long(
        "You stand in the peaceful temple of the Healers Guild. Soft light filters\n" +
        "through stained glass windows depicting scenes of mercy. The scent of\n" +
        "healing herbs fills the air. A serene priestess in white robes tends to\n" +
        "a small altar.\n\n" +
        "Type 'join' to become a member, or 'leave' to resign your membership.\n" +
        "Type 'learn' to see available spells, or 'learn <spell>' to study a spell."
    );

    // Magic schools granted by this guild
    set_granted_skills(({
        "abjuration",
        "divination"
    }));

    // Spells this guild can teach (players must use 'learn' command)
    set_taught_spells(({
        "/world/spells/heal",
        "/world/spells/shield"
    }));

    // No conflicting guilds - healers can also be fighters/mages
    set_conflicting_guilds(({ }));

    add_exit("west", "/world/rooms/town/temple_road_north");
}

// Override on_join to show helpful message
void on_join(object player) {
    // Call parent to grant skills
    ::on_join(player);

    tell_object(player, "\nThe priestess smiles warmly and places a hand on your shoulder.\n");
    tell_object(player, "\"Welcome, child. May you bring comfort to those in need.\"\n");
    tell_object(player, "\"I can teach you the arts of healing. Type 'learn' to see available spells.\"\n");
}

// Override on_leave - players keep learned spells but can no longer learn new ones
void on_leave(object player) {
    // Call parent to remove skill advancement ability
    ::on_leave(player);

    tell_object(player, "The priestess nods sadly.\n");
    tell_object(player, "\"Go with peace. You may keep the knowledge you have gained,\n");
    tell_object(player, "but I can teach you no more.\"\n");
}
