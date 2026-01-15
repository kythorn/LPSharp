// /cmds/std/learn.c
// Learn spells at guild locations
// Usage: learn         - list available spells
//        learn <spell> - attempt to learn a spell

void main(string args) {
    object player;
    object room;

    player = this_player();
    if (!player) return;

    room = environment(player);
    if (!room) {
        write("You can't learn anything here.\n");
        return;
    }

    // Check if room is a guild (has teach_spell function)
    if (!call_other(room, "query_guild_name")) {
        write("You need to be at a guild to learn spells.\n");
        return;
    }

    if (!args || args == "") {
        // List available spells
        call_other(room, "list_available_spells", player);
    } else {
        // Try to learn a specific spell
        call_other(room, "teach_spell", player, args);
    }
}
