// /cmds/std/guilds.c
// Display guild memberships

int main(string args) {
    object player;
    string *memberships;
    int i;

    player = this_player();
    if (!player) {
        return 0;
    }

    memberships = call_other(player, "query_guilds");

    if (!memberships || sizeof(memberships) == 0) {
        write("You are not a member of any guilds.\n");
        return 1;
    }

    write("You are a member of the following guilds:\n");
    for (i = 0; i < sizeof(memberships); i++) {
        object guild;
        string guild_name;

        guild = load_object(memberships[i]);
        if (guild) {
            guild_name = call_other(guild, "query_guild_name");
            write("  - " + guild_name + " (" + memberships[i] + ")\n");
        } else {
            write("  - " + memberships[i] + " (unavailable)\n");
        }
    }

    return 1;
}
