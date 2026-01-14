// /world/guilds/mages.c
// The Mages Guild - grants evocation and conjuration magic

inherit "/std/guild";

// Spells taught by this guild
string *granted_spells;

void create() {
    ::create();

    set_guild_name("Mages Guild");

    set_short("Mages Guild Tower");
    set_long("You stand in the grand tower of the Mages Guild. " +
        "Shelves lined with ancient tomes reach to the vaulted ceiling. " +
        "Arcane symbols are etched into the stone floor, faintly glowing. " +
        "An elderly wizard in star-covered robes studies at a large desk.\n\n" +
        "Type 'join' to become a member, or 'leave' to resign your membership.");

    // Magic schools granted by this guild
    set_granted_skills(({
        "evocation",
        "conjuration",
        "transmutation"
    }));

    // Spells taught when joining
    granted_spells = ({
        "/world/spells/magic_missile",
        "/world/spells/fireball"
    });

    // Can't be both a Mage and a Healer (for game balance)
    set_conflicting_guilds(({ }));

    add_exit("south", "/world/rooms/town/east_market");
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

    tell_object(player, "\nThe wizard stands and bows slightly.\n");
    tell_object(player, "\"Welcome, apprentice. Let the arcane arts guide you.\"\n");
    tell_object(player, "\"I have taught you the basics of offensive magic.\"\n");
    tell_object(player, "\nYou have learned: Magic Missile, Fireball\n");
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

    tell_object(player, "The wizard sighs sadly.\n");
    tell_object(player, "\"The knowledge fades from your mind...\"\n");
}
