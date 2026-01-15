// /cmds/std/chat.c
// Global chat channel command
//
// This is a wrapper around the generic channel handler.
// Usage:
//   chat <message>  - Send a message
//   chat on         - Enable this channel
//   chat off        - Disable this channel
//   chat history    - View recent history
//   chat history 50 - View last 50 messages

inherit "/cmds/std/_channel";

void create() {
    ::create();
    channel_name = "chat";
}
