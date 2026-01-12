// clone.c - Clone an object into your inventory
// Usage: clone <path>
// Creates a clone of the specified object and moves it to your inventory.

void main(string args) {
    if (args == "" || args == 0) {
        write("Usage: clone <path>");
        write("Example: clone /std/object");
        return;
    }

    object obj = clone_object(args);

    if (!obj) {
        write("Failed to clone: " + args);
        return;
    }

    // Try to move the clone to player's inventory
    object player = this_player();
    if (player) {
        obj->move(player);
        write("Cloned " + args + " -> " + object_name(obj));
    } else {
        write("Cloned " + args + " -> " + object_name(obj) + " (no inventory to move to)");
    }
}
