// /cmds/southwest.c - Move southwest
void main(string args) {
    object go_cmd;
    go_cmd = load_object("/cmds/go");
    call_other(go_cmd, "main", "southwest");
}
