# Chat System

The chat system provides multi-channel messaging with persistent history.

## Commands

### Channel Commands
Each channel has its own command with consistent subcommands:

```
<channel> <message>     Send a message
<channel> on            Subscribe to channel
<channel> off           Unsubscribe from channel
<channel> history [N]   View last N messages (default 20)
```

**Available channels:**
- `chat` - Global in-character chat
- `ooc` - Out-of-character chat

### Utility Commands

```
channels                List all channels and subscription status
history [channel] [N]   View history (all channels or specific)
```

## Architecture

### Files

| File | Purpose |
|------|---------|
| `/secure/daemon/chat.c` | Central daemon managing all channels |
| `/cmds/std/_channel.c` | Base class for channel commands |
| `/cmds/std/chat.c` | Global chat command (inherits _channel) |
| `/cmds/std/ooc.c` | OOC chat command (inherits _channel) |
| `/cmds/std/channels.c` | List available channels |
| `/cmds/std/history.c` | View chat history |
| `/std/player.c` | Player subscription storage |
| `/secure/data/chat_*.json` | Persistent history files |

### Chat Daemon (`/secure/daemon/chat.c`)

The daemon manages multiple channels. Each channel has:
- `name` - Unique identifier (e.g., "chat", "guild_fighters")
- `prefix` - Display prefix (e.g., "[Chat]", "[Fighters]")
- `history` - Array of message mappings
- `restricted` - If 1, requires permission check
- `permission_func` - Function to call on player to check access

**Key functions:**
```lpc
// Register a new channel
register_channel(name, prefix, restricted, permission_func)

// Unregister a channel
unregister_channel(name)

// Send a message (broadcasts to all subscribed players)
send_message(channel, sender, message)

// Get history
get_history(channel, count)
get_formatted_history(channel, count)

// Check player access
can_access(channel, player)
query_player_subscribed(player, channel)
```

### Player Integration (`/std/player.c`)

Players store subscriptions in a mapping:
```lpc
mapping chat_subscriptions;  // channel_name -> 1 (on) or 0 (off)
```

**Functions:**
```lpc
query_chat_subscriptions()           // Get all subscriptions
set_chat_subscription(channel, val)  // Set subscription (1=on, 0=off)
query_chat_subscription(channel)     // Check specific channel
```

Default behavior: Players are subscribed to all unrestricted channels unless explicitly unsubscribed.

## Adding New Channels

### 1. Register the Channel

In the chat daemon's `create()` or dynamically:
```lpc
register_channel("newchannel", "[New]", 0, "");
```

For restricted channels (e.g., guild chat):
```lpc
register_channel("guild_fighters", "[Fighters]", 1, "is_fighters_member");
```

### 2. Create the Command

Create `/cmds/std/newchannel.c`:
```lpc
inherit "/cmds/std/_channel";

void create() {
    ::create();
    channel_name = "newchannel";
}
```

### 3. Add Permission Function (if restricted)

In `/std/player.c` or a guild-specific file:
```lpc
int is_fighters_member() {
    return is_guild_member("/world/guilds/fighters");
}
```

## Guild Chat Example

To add chat for the Fighters Guild:

1. **Register channel** (in guild `create()` or chat daemon):
```lpc
chat_daemon = load_object("/secure/daemon/chat");
call_other(chat_daemon, "register_channel",
    "guild_fighters", "[Fighters]", 1, "is_fighters_member");
```

2. **Create command** `/cmds/std/gchat.c` (or `ftalk.c`):
```lpc
inherit "/cmds/std/_channel";

void create() {
    ::create();
    channel_name = "guild_fighters";
}
```

3. **Permission function** in `/std/player.c`:
```lpc
int is_fighters_member() {
    return is_guild_member("/world/guilds/fighters");
}
```

## History Persistence

History is saved to `/secure/data/chat_<channel>.json` in format:
```
timestamp|sender|message
timestamp|sender|message
```

Each channel maintains up to 100 messages (configurable via `max_history`).

## Message Format

Messages are stored as mappings:
```lpc
([
    "time": 1234567890,    // Unix timestamp
    "sender": "PlayerName",
    "message": "Hello world"
])
```

Display format: `[HH:MM] [Prefix] Sender: message`
