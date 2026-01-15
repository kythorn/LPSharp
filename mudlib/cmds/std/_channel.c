// /cmds/std/_channel.c
// Base class for channel commands
//
// USAGE:
// ------
// To create a new channel command, inherit this and set channel_name:
//
//   inherit "/cmds/std/_channel";
//   void create() {
//       ::create();
//       channel_name = "mychannel";
//   }
//
// The channel must be registered in /secure/daemon/chat first.
//
// COMMANDS:
// ---------
// <channel> <message>     - Send a message to the channel
// <channel> on            - Subscribe to the channel
// <channel> off           - Unsubscribe from the channel
// <channel> history [N]   - View last N messages (default 20)
//

string channel_name;

void create() {
    channel_name = "";
}

void main(string args) {
    object player;
    object chat_daemon;
    string sender;
    string *parts;
    string subcommand;
    int count;

    player = this_player();
    if (!player) {
        return;
    }

    if (!channel_name || channel_name == "") {
        write("Error: Channel not configured.\n");
        return;
    }

    // Load chat daemon
    chat_daemon = load_object("/secure/daemon/chat");
    if (!chat_daemon) {
        write("Chat system is unavailable.\n");
        return;
    }

    // Check if channel exists
    if (!call_other(chat_daemon, "query_channel", channel_name)) {
        write("Channel '" + channel_name + "' does not exist.\n");
        return;
    }

    // No args - show usage
    if (!args || args == "") {
        show_usage();
        return;
    }

    // Parse subcommand
    parts = explode(args, " ");
    subcommand = lower_case(parts[0]);

    // Handle subcommands
    if (subcommand == "on") {
        do_subscribe(player, chat_daemon);
        return;
    }

    if (subcommand == "off") {
        do_unsubscribe(player, chat_daemon);
        return;
    }

    if (subcommand == "history") {
        count = 20;
        if (sizeof(parts) > 1) {
            count = to_int(parts[1]);
            if (count <= 0) count = 20;
            if (count > 100) count = 100;
        }
        do_history(player, chat_daemon, count);
        return;
    }

    // Otherwise, treat as a message
    do_send(player, chat_daemon, args);
}

void show_usage() {
    write("Usage: " + channel_name + " <message>  - Send a message\n");
    write("       " + channel_name + " on         - Enable channel\n");
    write("       " + channel_name + " off        - Disable channel\n");
    write("       " + channel_name + " history    - View recent messages\n");
}

void do_subscribe(object player, object chat_daemon) {
    // Check access
    if (!call_other(chat_daemon, "can_access", channel_name, player)) {
        write("You don't have access to the " + channel_name + " channel.\n");
        return;
    }

    call_other(player, "set_chat_subscription", channel_name, 1);
    write("You have subscribed to " + channel_name + ".\n");
}

void do_unsubscribe(object player, object chat_daemon) {
    call_other(player, "set_chat_subscription", channel_name, 0);
    write("You have unsubscribed from " + channel_name + ".\n");
    write("Use '" + channel_name + " on' to re-enable, or '" + channel_name + " history' to catch up.\n");
}

void do_history(object player, object chat_daemon, int count) {
    string history;

    // Check access
    if (!call_other(chat_daemon, "can_access", channel_name, player)) {
        write("You don't have access to the " + channel_name + " channel.\n");
        return;
    }

    history = call_other(chat_daemon, "get_formatted_history", channel_name, count);

    if (!history || history == "") {
        write("No " + channel_name + " history available.\n");
        return;
    }

    write("=== " + capitalize(channel_name) + " History ===\n");
    write(history);
    write("=== End of History ===\n");
}

void do_send(object player, object chat_daemon, string message) {
    string sender;

    // Check access
    if (!call_other(chat_daemon, "can_access", channel_name, player)) {
        write("You don't have access to the " + channel_name + " channel.\n");
        return;
    }

    // Check if subscribed
    if (!call_other(chat_daemon, "query_player_subscribed", player, channel_name)) {
        write("You have " + channel_name + " disabled. Use '" + channel_name + " on' to enable.\n");
        return;
    }

    sender = call_other(player, "query_name");
    if (!call_other(chat_daemon, "send_message", channel_name, sender, message)) {
        write("Failed to send message.\n");
    }
}
