// /secure/daemon/chat.c
// Multi-channel chat system daemon
//
// ARCHITECTURE:
// =============
// This daemon manages multiple chat channels. Each channel has:
// - A unique name (e.g., "chat", "ooc", "guild_fighters")
// - Its own message history
// - A display prefix (e.g., "[Chat]", "[OOC]", "[Fighters]")
//
// CHANNELS:
// ---------
// Channels are stored in the `channels` mapping:
//   channels[name] = ([
//       "prefix": "[Chat]",           // Display prefix
//       "history": ({ ... }),         // Array of message mappings
//       "restricted": 0,              // If 1, requires permission check
//       "permission_func": "...",     // Function to call on player to check access
//   ])
//
// PLAYER INTEGRATION:
// -------------------
// Players track their subscriptions via:
//   query_chat_subscriptions() -> returns mapping of channel_name -> 1/0
//   set_chat_subscription(channel, enabled) -> sets subscription
//   query_chat_enabled() -> legacy, returns subscription to "chat" channel
//
// The daemon calls these functions when sending messages.
//
// ADDING NEW CHANNELS:
// --------------------
// 1. Call register_channel(name, prefix, restricted, permission_func) from create()
//    or dynamically (e.g., when a guild is created)
//
// 2. For restricted channels (like guilds), set restricted=1 and provide
//    permission_func (e.g., "is_fighters_guild_member")
//
// 3. Create commands that call send_message(channel, sender, message)
//
// EXAMPLE - Adding guild chat:
// ----------------------------
//   // In guild create():
//   chat = load_object("/secure/daemon/chat");
//   chat->register_channel("guild_fighters", "[Fighters]", 1, "is_fighters_member");
//
//   // The permission function on player:
//   int is_fighters_member() { return is_guild_member("/world/guilds/fighters"); }
//
// MESSAGE FORMAT:
// ---------------
// Messages are stored as mappings:
//   ([ "time": timestamp, "sender": "Name", "message": "text" ])
//
// History is saved to /secure/data/chat_<channel>.json
//

inherit "/std/object";

// Channel data: name -> channel info mapping
mapping channels;

// Configuration
int max_history;
string data_dir;

void create() {
    ::create();
    set_short("chat daemon");

    max_history = 100;
    data_dir = "/secure/data";
    channels = ([]);

    // Register default channels
    register_channel("chat", "[Chat]", 0, "");
    register_channel("ooc", "[OOC]", 0, "");

    // Load history for all channels
    load_all_history();
}

// Register a new channel
// name: unique channel identifier (e.g., "chat", "guild_fighters")
// prefix: display prefix (e.g., "[Chat]", "[Fighters]")
// restricted: if 1, checks permission_func before allowing send/receive
// permission_func: function name to call on player to check access (returns 1 if allowed)
void register_channel(string name, string prefix, int restricted, string permission_func) {
    if (!name || name == "") return;

    channels = channels + ([
        name: ([
            "prefix": prefix,
            "history": ({}),
            "restricted": restricted,
            "permission_func": permission_func
        ])
    ]);
}

// Unregister a channel (e.g., when a guild is disbanded)
void unregister_channel(string name) {
    mapping new_channels;
    string *names;
    int i;

    if (!channels[name]) return;

    new_channels = ([]);
    names = keys(channels);
    for (i = 0; i < sizeof(names); i++) {
        if (names[i] != name) {
            new_channels = new_channels + ([ names[i]: channels[names[i]] ]);
        }
    }
    channels = new_channels;
}

// Get list of all channel names
string *query_channels() {
    return keys(channels);
}

// Get channel info
mapping query_channel(string name) {
    return channels[name];
}

// Check if a player can access a channel
int can_access(string channel, object player) {
    mapping ch;
    string func;

    ch = channels[channel];
    if (!ch) return 0;

    // Unrestricted channels are open to all
    if (!ch["restricted"]) return 1;

    // Check permission function on player
    func = ch["permission_func"];
    if (!func || func == "") return 1;

    return call_other(player, func);
}

// Load history for a specific channel
void load_channel_history(string channel) {
    mapping ch;
    string data;
    string filename;

    ch = channels[channel];
    if (!ch) return;

    filename = data_dir + "/chat_" + channel + ".json";
    data = read_file(filename);

    if (data && data != "") {
        string *lines;
        mixed *history;
        int i;

        lines = explode(data, "\n");
        history = ({});

        for (i = 0; i < sizeof(lines); i++) {
            string *parts;
            if (lines[i] == "") continue;

            parts = explode(lines[i], "|");
            if (sizeof(parts) >= 3) {
                mapping entry;
                entry = ([
                    "time": to_int(parts[0]),
                    "sender": parts[1],
                    "message": implode(parts[2..], "|")
                ]);
                history = history + ({ entry });
            }
        }

        ch["history"] = history;
    }
}

// Load history for all channels
void load_all_history() {
    string *names;
    int i;

    names = keys(channels);
    for (i = 0; i < sizeof(names); i++) {
        load_channel_history(names[i]);
    }
}

// Save history for a specific channel
void save_channel_history(string channel) {
    mapping ch;
    mixed *history;
    string data;
    string filename;
    int i;

    ch = channels[channel];
    if (!ch) return;

    history = ch["history"];
    data = "";

    for (i = 0; i < sizeof(history); i++) {
        mapping entry;
        entry = history[i];
        data = data + entry["time"] + "|" + entry["sender"] + "|" + entry["message"] + "\n";
    }

    filename = data_dir + "/chat_" + channel + ".json";
    write_file(filename, data, 1);
}

// Send a message to a channel
// Returns 1 on success, 0 on failure
int send_message(string channel, string sender, string message) {
    mapping ch;
    mapping entry;
    mixed *history;
    object *players;
    string formatted;
    string prefix;
    int i;

    ch = channels[channel];
    if (!ch) return 0;

    if (!sender || sender == "" || !message || message == "") {
        return 0;
    }

    // Create history entry
    entry = ([
        "time": time(),
        "sender": sender,
        "message": message
    ]);

    // Add to history
    history = ch["history"];
    history = history + ({ entry });

    // Trim if too long
    if (sizeof(history) > max_history) {
        history = history[sizeof(history) - max_history..];
    }
    ch["history"] = history;

    // Save to disk
    save_channel_history(channel);

    // Format message
    prefix = ch["prefix"];
    formatted = prefix + " " + sender + ": " + message + "\n";

    // Send to all subscribed players who can access this channel
    players = users();
    for (i = 0; i < sizeof(players); i++) {
        if (players[i] &&
            query_player_subscribed(players[i], channel) &&
            can_access(channel, players[i])) {
            tell_object(players[i], formatted);
        }
    }

    return 1;
}

// Check if a player is subscribed to a channel
int query_player_subscribed(object player, string channel) {
    mapping subs;

    // Try the new subscription system first
    subs = call_other(player, "query_chat_subscriptions");
    if (subs) {
        // Check if channel key exists in mapping (member returns index or -1)
        if (member(subs, channel) != -1) {
            // Explicit subscription setting exists - return it directly
            return subs[channel];
        }
    }

    // Fall back to legacy query_chat_enabled for "chat" channel
    if (channel == "chat") {
        return call_other(player, "query_chat_enabled");
    }

    // Default: subscribed to unrestricted channels
    if (channels[channel] && !channels[channel]["restricted"]) {
        return 1;
    }

    return 0;
}

// Get history for a channel
// count = number of messages (0 = all)
mixed *get_history(string channel, int count) {
    mapping ch;
    mixed *history;

    ch = channels[channel];
    if (!ch) return ({});

    history = ch["history"];

    if (count <= 0 || count >= sizeof(history)) {
        return history;
    }

    return history[sizeof(history) - count..];
}

// Format a single history entry
string format_entry(string channel, mapping entry) {
    mapping ch;
    string time_str;
    string prefix;
    int t;
    int *tm;

    ch = channels[channel];
    prefix = ch ? ch["prefix"] : "[???]";

    t = entry["time"];
    tm = localtime(t);

    // Format as HH:MM
    time_str = "";
    if (tm[2] < 10) time_str = time_str + "0";
    time_str = time_str + tm[2] + ":";
    if (tm[1] < 10) time_str = time_str + "0";
    time_str = time_str + tm[1];

    return "[" + time_str + "] " + prefix + " " + entry["sender"] + ": " + entry["message"];
}

// Get formatted history as a string
string get_formatted_history(string channel, int count) {
    mixed *history;
    string result;
    int i;

    history = get_history(channel, count);
    result = "";

    for (i = 0; i < sizeof(history); i++) {
        result = result + format_entry(channel, history[i]) + "\n";
    }

    return result;
}
