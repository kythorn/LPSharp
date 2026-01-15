// /cmds/std/channels.c
// List available chat channels and subscription status

void main(string args) {
    object player;
    object chat_daemon;
    string *channel_names;
    int i;

    player = this_player();
    if (!player) {
        return;
    }

    // Load chat daemon
    chat_daemon = load_object("/secure/daemon/chat");
    if (!chat_daemon) {
        write("Chat system is unavailable.\n");
        return;
    }

    channel_names = call_other(chat_daemon, "query_channels");

    if (!channel_names || sizeof(channel_names) == 0) {
        write("No channels available.\n");
        return;
    }

    write("=== Available Channels ===\n");

    for (i = 0; i < sizeof(channel_names); i++) {
        string name;
        mapping info;
        int can_access;
        int subscribed;
        string status;

        name = channel_names[i];
        info = call_other(chat_daemon, "query_channel", name);
        can_access = call_other(chat_daemon, "can_access", name, player);

        if (!can_access) {
            status = "(no access)";
        } else {
            subscribed = call_other(chat_daemon, "query_player_subscribed", player, name);
            if (subscribed) {
                status = "[ON]";
            } else {
                status = "[OFF]";
            }
        }

        write("  " + name + " " + status + " - " + info["prefix"] + "\n");
    }

    write("\nUse '<channel> on/off' to toggle, '<channel> history' to view history.\n");
}
