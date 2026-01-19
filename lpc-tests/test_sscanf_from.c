// Test sscanf parsing for "get X from Y" commands

void run_tests() {
    string item;
    string container;
    int result;

    // Test 1: Simple case
    result = sscanf("sword from corpse", "%s from %s", item, container);
    assert(result == 2, "Test 1: sscanf should match 2 items");
    assert(item == "sword", "Test 1: item should be 'sword', got '" + item + "'");
    assert(container == "corpse", "Test 1: container should be 'corpse', got '" + container + "'");

    // Test 2: Numbered item
    result = sscanf("sword 2 from bag", "%s from %s", item, container);
    assert(result == 2, "Test 2: sscanf should match 2 items");
    assert(item == "sword 2", "Test 2: item should be 'sword 2', got '" + item + "'");
    assert(container == "bag", "Test 2: container should be 'bag', got '" + container + "'");

    // Test 3: Numbered container
    result = sscanf("gold from bag 2", "%s from %s", item, container);
    assert(result == 2, "Test 3: sscanf should match 2 items");
    assert(item == "gold", "Test 3: item should be 'gold', got '" + item + "'");
    assert(container == "bag 2", "Test 3: container should be 'bag 2', got '" + container + "'");

    // Test 4: Both numbered
    result = sscanf("sword 3 from bag 2", "%s from %s", item, container);
    assert(result == 2, "Test 4: sscanf should match 2 items");
    assert(item == "sword 3", "Test 4: item should be 'sword 3', got '" + item + "'");
    assert(container == "bag 2", "Test 4: container should be 'bag 2', got '" + container + "'");

    // Test 5: "all from container"
    result = sscanf("all from corpse", "%s from %s", item, container);
    assert(result == 2, "Test 5: sscanf should match 2 items");
    assert(item == "all", "Test 5: item should be 'all', got '" + item + "'");
    assert(container == "corpse", "Test 5: container should be 'corpse', got '" + container + "'");

    write("All sscanf 'from' tests passed!");
}
