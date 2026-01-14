// pwd.c - Print current working directory
// Usage: pwd

void main(string args) {
    object player;
    string path;

    player = this_player();
    if (!player) {
        write("No player object!");
        return;
    }

    path = call_other(player, "query_cwd");
    if (!path || path == "") {
        path = "/";
    }

    write(path);
}
