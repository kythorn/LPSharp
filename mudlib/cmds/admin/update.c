/*
 * /cmds/admin/update.c - Hot-reload command for admins
 *
 * Usage: update <path>
 *
 * Recompiles an object and all objects that inherit from it.
 * Existing clones keep their old code (conservative strategy).
 */

void main(string arg) {
    int count;

    if (!arg || arg == "") {
        write("Usage: update <path>\n");
        write("  Example: update /std/object\n");
        write("  Example: update /obj/weapons/sword\n");
        return;
    }

    write("Updating " + arg + "...\n");

    count = update(arg);

    if (count > 0) {
        write("Successfully updated " + count + " object(s).\n");
    } else {
        write("Update failed or no objects found.\n");
    }
}
