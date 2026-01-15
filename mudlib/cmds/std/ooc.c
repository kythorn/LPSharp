// /cmds/std/ooc.c
// Out-of-character chat channel
//
// Usage:
//   ooc <message>  - Send a message
//   ooc on         - Enable this channel
//   ooc off        - Disable this channel
//   ooc history    - View recent history

inherit "/cmds/std/_channel";

void create() {
    ::create();
    channel_name = "ooc";
}
