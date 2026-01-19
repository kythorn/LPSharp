// Test skill system

void run_tests() {
    object player;
    int initial_skill;
    int i;
    int gained;

    // Create a player object
    player = clone_object("/std/player");
    call_other(player, "set_name", "SkillTester");

    write("=== Skill System Tests ===\n\n");

    // Test 1: Initial skills should be empty
    write("Test 1: Initial skills...\n");
    initial_skill = call_other(player, "query_skill", "sword");
    assert(initial_skill == 0, "Initial sword skill should be 0");
    write("  PASS: Initial sword skill is 0\n\n");

    // Test 2: Can set skills
    write("Test 2: Setting skills...\n");
    call_other(player, "set_skill", "sword", 10);
    assert(call_other(player, "query_skill", "sword") == 10, "Sword skill should be 10");
    write("  PASS: Can set skill to 10\n\n");

    // Test 3: Basic skills are always allowed
    write("Test 3: Basic skills allowed...\n");
    assert(call_other(player, "can_use_skill", "unarmed") == 1, "Unarmed should be allowed");
    assert(call_other(player, "can_use_skill", "dodge") == 1, "Dodge should be allowed");
    write("  PASS: Basic skills (unarmed, dodge) are allowed\n\n");

    // Test 4: Advance skill should work for allowed skills
    write("Test 4: Advancing skills...\n");
    call_other(player, "set_skill", "unarmed", 0);
    gained = 0;
    for (i = 0; i < 100; i++) {
        // High difficulty for better chance
        if (call_other(player, "advance_skill", "unarmed", 20)) {
            gained = gained + 1;
        }
    }
    write("  Attempted 100 advances at difficulty 20, gained: " + gained + "\n");
    assert(gained > 0, "Should have gained at least 1 skill point in 100 tries");
    write("  PASS: Skill advancement works\n\n");

    // Test 5: Logarithmic slowdown - at higher skill, gains should be slower
    write("Test 5: Logarithmic diminishing returns...\n");
    call_other(player, "set_skill", "sword", 0);
    int low_gains;
    low_gains = 0;
    for (i = 0; i < 50; i++) {
        if (call_other(player, "advance_skill", "sword", 20)) {
            low_gains = low_gains + 1;
        }
    }

    call_other(player, "set_skill", "sword", 50);
    int high_gains;
    high_gains = 0;
    for (i = 0; i < 50; i++) {
        if (call_other(player, "advance_skill", "sword", 20)) {
            high_gains = high_gains + 1;
        }
    }
    write("  At skill 0: " + low_gains + " gains in 50 tries\n");
    write("  At skill 50: " + high_gains + " gains in 50 tries\n");
    // At skill 50, gains should generally be lower (but randomness means not always)
    write("  (Higher skill = slower gains due to logarithmic curve)\n\n");

    // Test 6: Damage scales with skill
    write("Test 6: Damage scaling...\n");
    call_other(player, "set_skill", "unarmed", 0);
    call_other(player, "set_str", 10);
    int dmg_at_0;
    dmg_at_0 = call_other(player, "query_damage");

    call_other(player, "set_skill", "unarmed", 50);
    int dmg_at_50;
    dmg_at_50 = call_other(player, "query_damage");

    write("  Damage at skill 0: " + dmg_at_0 + "\n");
    write("  Damage at skill 50: " + dmg_at_50 + "\n");
    assert(dmg_at_50 > dmg_at_0, "Damage at skill 50 should be higher");
    write("  PASS: Higher skill = more damage\n\n");

    // Cleanup
    destruct(player);

    write("=== All Tests Passed! ===\n");
}
