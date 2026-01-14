// /world/guilds/fighters.c
// The Fighters Guild - grants combat skills

inherit "/std/guild";

void create() {
    ::create();

    set_guild_name("Fighters Guild");

    set_short("Fighters Guild Hall");
    set_long("You are standing in the grand hall of the Fighters Guild. " +
        "Weapons of all kinds adorn the walls - swords, axes, maces, and more. " +
        "Training dummies stand in corners, showing signs of heavy use. " +
        "A grizzled veteran sits at a desk, ready to accept new recruits.\n\n" +
        "Type 'join' to become a member, or 'leave' to resign your membership.");

    // Skills granted by this guild
    set_granted_skills(({
        "sword",
        "axe",
        "mace",
        "shield_block",
        "parry"
    }));

    // No conflicting guilds for now - fighters can also be in other guilds
    set_conflicting_guilds(({ }));

    add_exit("south", "/world/rooms/town/square");
}

// Override can_join to add any special requirements
int can_join(object player) {
    // First check parent class (conflicting guilds)
    if (!::can_join(player)) {
        return 0;
    }

    // Fighters guild has no additional requirements
    // Could add level/stat requirements here in the future
    return 1;
}

// Override on_join for special welcome message
void on_join(object player) {
    ::on_join(player);

    tell_object(player, "\nThe veteran stands and clasps your forearm.\n");
    tell_object(player, "\"Welcome, warrior. Train hard and fight with honor.\"\n");
}
