// dest.c - Destruct an object
// Usage: dest <object_name>
// Destroys the named object.

void main(string args) {
    if (args == "" || args == 0) {
        write("Usage: dest <object_name>");
        write("Example: dest /std/object#1234");
        return;
    }

    object obj = find_object(args);

    if (!obj) {
        write("Object not found: " + args);
        return;
    }

    string name = object_name(obj);
    destruct(obj);
    write("Destructed: " + name);
}
