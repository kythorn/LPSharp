// /cmds/std/score.c
// Display player stats and status

int main(string args) {
    object player;
    string output;
    object weapon;
    mapping armor;
    string *slots;
    int i;

    player = this_player();
    if (!player) {
        return 0;
    }

    output = "";

    // Name and level
    output = output + call_other(player, "query_name") + " - Level " +
             call_other(player, "query_level") + "\n";
    output = output + "----------------------------------------\n";

    // HP and Mana
    output = output + "HP: " + call_other(player, "query_hp") + "/" +
             call_other(player, "query_max_hp") + "    ";
    output = output + "Mana: " + call_other(player, "query_mana") + "/" +
             call_other(player, "query_max_mana") + "\n\n";

    // Stats
    output = output + "Stats:\n";
    output = output + "  STR: " + call_other(player, "query_str") + "    ";
    output = output + "DEX: " + call_other(player, "query_dex") + "    ";
    output = output + "AGI: " + call_other(player, "query_agi") + "\n";
    output = output + "  CON: " + call_other(player, "query_con") + "    ";
    output = output + "INT: " + call_other(player, "query_int") + "    ";
    output = output + "WIS: " + call_other(player, "query_wis") + "\n";
    output = output + "  CHA: " + call_other(player, "query_cha") + "\n\n";

    // XP and Gold
    output = output + "XP: " + call_other(player, "query_xp") + "    ";
    output = output + "Gold: " + call_other(player, "query_gold") + "\n\n";

    // Equipment - Wielded weapon
    output = output + "Equipment:\n";
    weapon = call_other(player, "query_wielded");
    if (weapon) {
        output = output + "  Weapon: " + call_other(weapon, "query_short") + "\n";
    } else {
        output = output + "  Weapon: (bare hands)\n";
    }

    // Worn armor
    armor = call_other(player, "query_worn_armor");
    if (armor && sizeof(armor) > 0) {
        slots = keys(armor);
        for (i = 0; i < sizeof(slots); i++) {
            object piece;
            piece = armor[slots[i]];
            if (piece) {
                output = output + "  " + capitalize(slots[i]) + ": " +
                         call_other(piece, "query_short") + "\n";
            }
        }
    }

    // Total armor value
    output = output + "  Armor: " + call_other(player, "query_total_armor") + "\n";

    write(output);
    return 1;
}
