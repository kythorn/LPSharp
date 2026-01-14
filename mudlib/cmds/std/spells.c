// /cmds/std/spells.c
// Display known spells

int main(string args) {
    object player;
    string *known_spells;
    int i;

    player = this_player();
    if (!player) {
        return 0;
    }

    known_spells = call_other(player, "query_known_spells");

    if (!known_spells || sizeof(known_spells) == 0) {
        write("You don't know any spells.\n");
        write("Join a mage guild to learn magic!\n");
        return 1;
    }

    write("Your known spells:\n");
    write("==================\n");

    for (i = 0; i < sizeof(known_spells); i++) {
        object spell;
        string spell_path;

        spell_path = known_spells[i];
        spell = load_object(spell_path);

        if (spell) {
            string info;
            info = call_other(spell, "query_spell_info");
            write(info);
        } else {
            write("  " + spell_path + " (unavailable)\n");
        }
    }

    // Show mana status
    int mana;
    int max_mana;
    mana = call_other(player, "query_mana");
    max_mana = call_other(player, "query_max_mana");
    write("\nMana: " + mana + "/" + max_mana + "\n");

    return 1;
}
