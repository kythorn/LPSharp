// /cmds/std/consider.c
// Evaluate how a fight against a target would go

void main(string args) {
    object player;
    object target;
    object room;
    object *contents;
    int i;

    // Player stats
    int player_hp;
    int player_damage;
    int player_hit_chance;
    int player_armor;

    // Target stats
    int target_hp;
    int target_damage;
    int target_hit_chance;
    int target_armor;

    // Calculated values
    int player_dpr;  // damage per round
    int target_dpr;
    int rounds_to_kill_target;
    int rounds_to_kill_player;
    int advantage;
    string rating;
    string target_name;

    player = this_player();
    if (!player) {
        return;
    }

    if (!args || args == "") {
        write("Consider whom?\n");
        return;
    }

    // Find target in room
    room = environment(player);
    if (!room) {
        write("You're not anywhere.\n");
        return;
    }

    contents = all_inventory(room);
    target = 0;

    for (i = 0; i < sizeof(contents); i++) {
        if (contents[i] != player && call_other(contents[i], "id", args)) {
            target = contents[i];
            break;
        }
    }

    if (!target) {
        write("You don't see that here.\n");
        return;
    }

    // Check if target is living
    if (!call_other(target, "is_living")) {
        write("That's not something you can fight.\n");
        return;
    }

    target_name = call_other(target, "query_short");

    // Get player combat stats
    player_hp = call_other(player, "query_hp");
    player_damage = call_other(player, "query_damage");
    player_hit_chance = call_other(player, "query_hit_chance", target);
    player_armor = call_other(player, "query_total_armor");

    // Get target combat stats
    target_hp = call_other(target, "query_hp");
    target_damage = call_other(target, "query_damage");
    target_hit_chance = call_other(target, "query_hit_chance", player);
    target_armor = call_other(target, "query_total_armor");

    // Clamp hit chances to reasonable range
    if (player_hit_chance < 5) player_hit_chance = 5;
    if (player_hit_chance > 95) player_hit_chance = 95;
    if (target_hit_chance < 5) target_hit_chance = 5;
    if (target_hit_chance > 95) target_hit_chance = 95;

    // Calculate expected damage per round (hit_chance% * (damage - armor), min 1)
    player_dpr = (player_hit_chance * (player_damage - target_armor)) / 100;
    if (player_dpr < 1) player_dpr = 1;

    target_dpr = (target_hit_chance * (target_damage - player_armor)) / 100;
    if (target_dpr < 1) target_dpr = 1;

    // Calculate rounds needed to kill each other
    rounds_to_kill_target = (target_hp + player_dpr - 1) / player_dpr;  // ceiling division
    rounds_to_kill_player = (player_hp + target_dpr - 1) / target_dpr;

    // Calculate advantage ratio (positive = player advantage)
    // How many more rounds do they need to kill us vs we need to kill them
    advantage = rounds_to_kill_player - rounds_to_kill_target;

    // Determine rating based on advantage
    if (advantage >= 20) {
        rating = "is a complete pushover.";
    } else if (advantage >= 10) {
        rating = "looks like easy prey.";
    } else if (advantage >= 5) {
        rating = "should be a comfortable fight.";
    } else if (advantage >= 2) {
        rating = "looks like a fair challenge.";
    } else if (advantage >= -2) {
        rating = "is evenly matched with you.";
    } else if (advantage >= -5) {
        rating = "looks like a tough fight.";
    } else if (advantage >= -10) {
        rating = "would be very dangerous to fight.";
    } else if (advantage >= -20) {
        rating = "would probably kill you.";
    } else {
        rating = "would annihilate you.";
    }

    write("You consider " + target_name + "...\n");
    write(capitalize(target_name) + " " + rating + "\n");
}
