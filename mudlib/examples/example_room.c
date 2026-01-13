// ============================================================================
// EXAMPLE ROOM - A template showing how to create rooms in LPMud Revival
// ============================================================================
//
// Rooms are environments that contain players, monsters, and items.
// All rooms inherit from /std/room.c which provides the basic functionality.
//
// To create your own room:
// 1. Copy this file to your desired location (e.g., /world/rooms/myarea/myroom.c)
// 2. Modify the create() function to set up your room
// 3. Add exits to connect to other rooms
// 4. Optionally add monster spawns, items, or special features
//
// ============================================================================

// Required: All rooms must inherit from /std/room.c
inherit "/std/room";

// ============================================================================
// create() - Called once when the room is first loaded
// ============================================================================
// This is where you set up all the static properties of your room.
// It runs only once when the object is first created/loaded.

void create() {
    // IMPORTANT: Always call the parent's create() first!
    // This initializes the base room functionality.
    ::create();

    // -------------------------------------------------------------------------
    // BASIC DESCRIPTIONS
    // -------------------------------------------------------------------------

    // set_short(string) - The room's title, shown at the top of the room display
    // Keep it concise but descriptive (2-6 words)
    set_short("A Cozy Example Room");

    // set_long(string) - The full room description players see when they look
    // This is your chance to paint a picture! Include:
    // - Visual details (colors, lighting, size)
    // - Atmospheric elements (sounds, smells)
    // - Points of interest that hint at things to do
    // - Environmental storytelling
    set_long(
        "This is an example room demonstrating how to create areas in LPMud Revival. "
        "Stone walls surround you, covered with patches of green moss. Flickering "
        "torches mounted in iron sconces cast dancing shadows across the floor. "
        "The air smells faintly of earth and old stone. A worn wooden sign hangs "
        "on the eastern wall."
    );

    // -------------------------------------------------------------------------
    // EXITS - Connections to other rooms
    // -------------------------------------------------------------------------

    // add_exit(direction, path) - Add a visible exit
    // Directions: "north", "south", "east", "west", "northeast", "northwest",
    //            "southeast", "southwest", "up", "down"
    // Path: Full path to the destination room file (without .c extension)

    add_exit("north", "/world/rooms/town/square");
    add_exit("east", "/world/rooms/wilderness/crossroads");

    // add_hidden_exit(direction, path) - Add a hidden exit
    // Hidden exits work but don't appear in "Obvious exits:"
    // Players must discover them through exploration or hints
    // NOTE: Currently only ONE hidden exit per room is supported
    add_hidden_exit("down", "/world/rooms/wilderness/ruins/crypt_entrance");

    // -------------------------------------------------------------------------
    // MONSTER SPAWNS
    // -------------------------------------------------------------------------

    // Rooms can automatically spawn monsters when they reset.
    // Use enable_reset() to turn on the reset timer, then add_spawn() for each
    // monster type that should appear.

    // enable_reset(seconds) - Enable periodic room resets
    // The room's reset() function will be called every N seconds.
    // Common values: 60 (1 min), 120 (2 min), 300 (5 min)
    enable_reset(60);

    // add_spawn(path) - Add a monster to spawn on reset
    // The room checks if a monster of this type is already present.
    // If not, it clones a new one and moves it here.
    add_spawn("/world/mobs/rat");

    // For multiple different monsters:
    // add_spawn("/world/mobs/spider");
    // add_spawn("/world/mobs/snake");

    // Or use set_spawns() to set all at once:
    // set_spawns(({ "/world/mobs/rat", "/world/mobs/spider" }));
}

// ============================================================================
// init() - Called when something enters the room (optional)
// ============================================================================
// Override this to add special behavior when players or objects enter.
// Called AFTER the object has been moved into the room.

void init() {
    // Always call parent init first!
    ::init();

    // You can add special messages or actions here
    // Example: Greet players who enter
    if (this_player()) {
        // tell_object sends a message only to a specific player
        tell_object(this_player(), "You feel a strange tingling sensation.\n");
    }

    // Example: Add a custom command available only in this room
    // add_action("do_read_sign", "read");
}

// ============================================================================
// reset() - Called periodically to refresh the room (optional)
// ============================================================================
// The base room.c reset() handles monster spawning automatically.
// Override this only if you need additional reset behavior.

void reset() {
    // Call parent to handle monster spawning
    ::reset();

    // Add any custom reset behavior here
    // Example: Spawn a special item
    // if (!present("gold_key", this_object())) {
    //     clone_object("/world/items/misc/gold_key")->move(this_object());
    // }
}

// ============================================================================
// Custom functions (optional)
// ============================================================================
// You can add any custom functions your room needs.

// Example: A readable sign
int do_read_sign(string arg) {
    if (arg != "sign" && arg != "wooden sign") {
        return 0;  // Not us, let other handlers try
    }

    write("The sign reads:\n");
    write("  'Welcome to the Example Room!'\n");
    write("  'May your adventures be bug-free.'\n");
    return 1;  // Command handled
}
