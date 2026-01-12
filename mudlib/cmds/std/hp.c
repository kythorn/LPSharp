// /cmds/std/hp.c
// Quick status display

void main(string args) {
    object player;
    int hp;
    int max_hp;
    int pct;
    string bar;
    string status;

    player = this_player();
    if (!player) {
        write("You have no physical form.");
        return;
    }

    hp = call_other(player, "query_hp");
    max_hp = call_other(player, "query_max_hp");

    // Calculate percentage
    if (max_hp > 0) {
        pct = (hp * 100) / max_hp;
    } else {
        pct = 0;
    }

    // Build a visual bar (20 chars wide)
    int filled;
    int i;
    filled = (hp * 20) / max_hp;
    if (filled > 20) filled = 20;
    if (filled < 0) filled = 0;

    bar = "[";
    for (i = 0; i < 20; i++) {
        if (i < filled) {
            bar = bar + "=";
        } else {
            bar = bar + " ";
        }
    }
    bar = bar + "]";

    // Status text
    if (pct == 100) {
        status = "Perfect health";
    } else if (pct >= 75) {
        status = "Slightly wounded";
    } else if (pct >= 50) {
        status = "Wounded";
    } else if (pct >= 25) {
        status = "Badly wounded";
    } else {
        status = "Near death!";
    }

    // Check combat status
    string combat_status;
    combat_status = "";
    if (call_other(player, "query_in_combat")) {
        object enemy;
        enemy = call_other(player, "query_attacker");
        if (enemy) {
            combat_status = " [Fighting: " + call_other(enemy, "query_short") + "]";
        }
    }

    write("HP: " + hp + "/" + max_hp + " " + bar + " " + status + combat_status);
}
