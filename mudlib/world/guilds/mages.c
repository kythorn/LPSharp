// /world/guilds/mages.c
// The Mages Guild - grants evocation and conjuration magic

inherit "/std/guild";

void create() {
    ::create();

    set_guild_name("Mages Guild");

    set_short("Mages Guild Tower");
    set_long(
        "You stand in the grand tower of the Mages Guild. Shelves lined with\n" +
        "ancient tomes reach to the vaulted ceiling. Arcane symbols are etched\n" +
        "into the stone floor, faintly glowing. An elderly wizard in star-covered\n" +
        "robes studies at a large desk.\n\n" +
        "Type 'join' to become a member, or 'leave' to resign your membership.\n" +
        "Type 'learn' to see available spells, or 'learn <spell>' to study a spell."
    );

    // Magic schools granted by this guild
    set_granted_skills(({
        "evocation",
        "conjuration",
        "transmutation"
    }));

    // Spells this guild can teach (players must use 'learn' command)
    set_taught_spells(({
        "/world/spells/magic_missile",
        "/world/spells/fireball"
    }));

    // No conflicting guilds
    set_conflicting_guilds(({ }));

    add_exit("south", "/world/rooms/town/east_market");
}

// Override on_join to show helpful message
void on_join(object player) {
    // Call parent to grant skills
    ::on_join(player);

    tell_object(player, "\nThe wizard stands and bows slightly.\n");
    tell_object(player, "\"Welcome, apprentice. Let the arcane arts guide you.\"\n");
    tell_object(player, "\"I can teach you the ways of destruction. Type 'learn' to see available spells.\"\n");
}

// Override on_leave - players keep learned spells but can no longer learn new ones
void on_leave(object player) {
    // Call parent to remove skill advancement ability
    ::on_leave(player);

    tell_object(player, "The wizard nods solemnly.\n");
    tell_object(player, "\"The knowledge you've gained remains with you,\n");
    tell_object(player, "but I can teach you nothing more.\"\n");
}
