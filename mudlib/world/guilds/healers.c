// /world/guilds/healers.c
// The Healers Guild - grants abjuration and divination magic

inherit "/std/guild";

// Spells taught by this guild
string *granted_spells;

void create() {
    ::create();

    set_guild_name("Healers Guild");

    set_short("Healers Guild Temple");
    set_long("You stand in the peaceful temple of the Healers Guild. " +
        "Soft light filters through stained glass windows depicting scenes of mercy. " +
        "The scent of healing herbs fills the air. " +
        "A serene priestess in white robes tends to a small altar.\n\n" +
        "Type 'join' to become a member, or 'leave' to resign your membership.");

    // Magic schools granted by this guild
    set_granted_skills(({
        "abjuration",
        "divination"
    }));

    // Spells taught when joining
    granted_spells = ({
        "/world/spells/heal",
        "/world/spells/shield"
    });

    // No conflicting guilds - healers can also be fighters/mages
    set_conflicting_guilds(({ }));

    add_exit("west", "/world/rooms/town/temple_road_north");
}

// Override on_join to also teach spells
void on_join(object player) {
    int i;

    // Call parent to grant skills
    ::on_join(player);

    // Teach spells
    for (i = 0; i < sizeof(granted_spells); i++) {
        call_other(player, "learn_spell", granted_spells[i]);
    }

    tell_object(player, "\nThe priestess smiles warmly and places a hand on your shoulder.\n");
    tell_object(player, "\"Welcome, child. May you bring comfort to those in need.\"\n");
    tell_object(player, "\"I have taught you the arts of protection and healing.\"\n");
    tell_object(player, "\nYou have learned: Heal, Shield\n");
    tell_object(player, "Type 'spells' to see your known spells.\n");
}

// Override on_leave to remove spells
void on_leave(object player) {
    int i;

    // Call parent to remove skills
    ::on_leave(player);

    // Remove spells
    for (i = 0; i < sizeof(granted_spells); i++) {
        call_other(player, "forget_spell", granted_spells[i]);
    }

    tell_object(player, "The priestess nods sadly.\n");
    tell_object(player, "\"Go with peace, but the sacred knowledge must remain here.\"\n");
}
