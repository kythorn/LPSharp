// /cmds/std/history.c
// View recent chat history (shortcut for 'chat history')
//
// Usage:
//   history          - View last 20 messages from all subscribed channels
//   history 50       - View last 50 messages
//   history chat     - View chat channel only
//   history ooc 30   - View last 30 OOC messages

void main(string args) {
    object player;
    object chat_daemon;
    string *parts;
    string channel;
    int count;
    string *channels_list;
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

    // Parse arguments
    count = 20;
    channel = "";

    if (args && args != "") {
        parts = explode(args, " ");

        for (i = 0; i < sizeof(parts); i++) {
            int num;
            num = to_int(parts[i]);
            if (num > 0) {
                count = num;
                if (count > 100) count = 100;
            } else if (parts[i] != "") {
                channel = parts[i];
            }
        }
    }

    // If specific channel requested
    if (channel != "") {
        if (!call_other(chat_daemon, "query_channel", channel)) {
            write("Unknown channel: " + channel + "\n");
            return;
        }
        if (!call_other(chat_daemon, "can_access", channel, player)) {
            write("You don't have access to the " + channel + " channel.\n");
            return;
        }

        string history;
        history = call_other(chat_daemon, "get_formatted_history", channel, count);
        if (!history || history == "") {
            write("No " + channel + " history available.\n");
            return;
        }
        write("=== " + capitalize(channel) + " History ===\n");
        write(history);
        write("=== End of History ===\n");
        return;
    }

    // Show history from all accessible channels
    channels_list = call_other(chat_daemon, "query_channels");
    write("=== Recent Chat History ===\n");

    for (i = 0; i < sizeof(channels_list); i++) {
        string ch_name;
        string history;

        ch_name = channels_list[i];

        if (!call_other(chat_daemon, "can_access", ch_name, player)) {
            continue;
        }

        history = call_other(chat_daemon, "get_formatted_history", ch_name, count);
        if (history && history != "") {
            write(history);
        }
    }

    write("=== End of History ===\n");
}
