// /cmds/d.c - Move down (shortcut)
void main(string args) {
    object go_cmd;
    go_cmd = load_object("/cmds/std/go");
    call_other(go_cmd, "main", "down");
}
