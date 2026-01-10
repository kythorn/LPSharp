// /cmds/north.c - Move north
void main(string args) {
    object look_cmd;
    look_cmd = load_object("/cmds/std/go");
    call_other(look_cmd, "main", "north");
}
