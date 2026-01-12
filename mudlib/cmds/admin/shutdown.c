// /cmds/admin/shutdown.c
// Shutdown command - gracefully shut down the server (Admin only)

void main(string args) {
    write("Initiating server shutdown...");

    // The driver will handle the actual shutdown via the shutdown() efun
    shutdown();
}
