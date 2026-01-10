// /cmds/loop.c
// Test command for infinite loop detection

void main(string args) {
    write("Starting infinite loop test...");
    while (1) {
        // This should be caught by execution limits
    }
    write("This should never be printed.");
}
