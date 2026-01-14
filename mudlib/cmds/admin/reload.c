// reload.c - Hot-reload all changed .c files
// Usage: reload
// Compares file modification times against loaded blueprints
// and reloads any that have changed on disk.

void main(string args) {
    string *reloaded;
    int i;

    write("Checking for changed files...\n");

    reloaded = reload_changed();

    if (sizeof(reloaded) == 0) {
        write("No files have changed.\n");
        return;
    }

    write("Reloaded " + sizeof(reloaded) + " file(s):\n");
    for (i = 0; i < sizeof(reloaded); i++) {
        write("  " + reloaded[i] + "\n");
    }
}
