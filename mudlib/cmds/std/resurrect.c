// /cmds/std/resurrect.c
// Return from the netherworld to the land of the living

int main(string args) {
    object player;
    object env;
    object town_square;
    string env_path;

    player = this_player();
    if (!player) {
        return 0;
    }

    // Check if player is in the netherworld
    env = environment(player);
    if (!env) {
        write("You can't resurrect here.\n");
        return 1;
    }

    env_path = file_name(env);
    if (env_path != "/world/rooms/special/netherworld") {
        write("You can only resurrect from the netherworld.\n");
        return 1;
    }

    // Resurrect the player
    write("You feel a warm light pulling you back to the mortal realm...\n");
    write("Your spirit returns to your body.\n\n");

    // Move to town square
    town_square = load_object("/world/rooms/town/town_square");
    if (town_square) {
        move_object(player, town_square);
        tell_room(town_square, call_other(player, "query_name") +
                  " has returned from the dead!\n", player);
    }

    // Save player state
    call_other(player, "save_player");

    return 1;
}
