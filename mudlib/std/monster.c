// /std/monster.c
// Base class for NPC monsters

inherit "/std/living";

string monster_name;
int aggressive;
int xp_value;
string *drop_items;
int drop_chance;  // Percentage chance to drop (default 100)

void create() {
    ::create();
    set_short("a monster");
    monster_name = "monster";
    aggressive = 0;
    xp_value = 10;
    drop_items = ({});
    drop_chance = 100;
}

// Add an item that this monster drops on death
void add_drop(string item_path) {
    drop_items = drop_items + ({ item_path });
}

// Set multiple drops at once
void set_drops(string *items) {
    drop_items = items;
}

// Set drop chance percentage (0-100)
void set_drop_chance(int chance) {
    drop_chance = chance;
}

// Create drop items in monster's inventory (called after create)
void setup_drops() {
    int i;
    object item;

    for (i = 0; i < sizeof(drop_items); i++) {
        // Check drop chance
        if (random(100) < drop_chance) {
            item = clone_object(drop_items[i]);
            if (item) {
                move_object(item, this_object());
            }
        }
    }
}

string query_name() {
    return monster_name;
}

void set_name(string n) {
    monster_name = n;
}

// Override id() to also match monster name
int id(string str) {
    if (!str || str == "") return 0;

    // Check monster_name (exact match, case insensitive)
    if (monster_name && lower_case(str) == lower_case(monster_name)) {
        return 1;
    }

    // Fall back to parent id() check (matches short description)
    return ::id(str);
}

int query_aggressive() {
    return aggressive;
}

void set_aggressive(int val) {
    aggressive = val;
}

int query_xp_value() {
    return xp_value;
}

void set_xp_value(int val) {
    xp_value = val;
}

// Called when something enters the monster's environment
// or when the monster enters an environment
void init() {
    object player;

    ::init();

    player = this_player();

    // If aggressive, attack any player that enters
    if (aggressive) {
        if (player && player != this_object()) {
            // Check if player is a living and not already fighting
            if (call_other(player, "is_living") && !query_in_combat()) {
                // Announce aggression
                tell_object(player, capitalize(query_short()) + " attacks you!\n");

                // Tell the room
                object room;
                room = environment(this_object());
                if (room) {
                    object *others;
                    int i;
                    others = all_inventory(room);
                    for (i = 0; i < sizeof(others); i++) {
                        if (others[i] != player && others[i] != this_object()) {
                            if (call_other(others[i], "is_living")) {
                                tell_object(others[i], capitalize(query_short()) + " attacks " + call_other(player, "query_name") + "!\n");
                            }
                        }
                    }
                }

                // Start combat
                start_combat(player);
            }
        }
    }
}

// Override die() for monster death behavior
void die() {
    object room;
    object killer;
    object corpse;
    object *inventory;
    int i;

    room = environment(this_object());
    killer = query_attacker();

    // Stop combat first
    stop_combat();

    // Award XP to killer if it's a player
    if (killer && call_other(killer, "is_living")) {
        if (call_other(killer, "query_level")) {  // Players have levels, monsters don't
            call_other(killer, "add_xp", xp_value);
            tell_object(killer, "You gain " + xp_value + " experience points.\n");
        }
    }

    // Announce death
    if (room) {
        tell_room(room, capitalize(query_short()) + " dies!\n");
    }

    // Create corpse
    corpse = clone_object("/std/corpse");
    call_other(corpse, "set_corpse_name", monster_name);

    // Move inventory to corpse
    inventory = all_inventory(this_object());
    for (i = 0; i < sizeof(inventory); i++) {
        move_object(inventory[i], corpse);
    }

    // Place corpse in room
    if (room) {
        move_object(corpse, room);
        call_other(corpse, "start_decay");
    }

    // Destruct the monster
    destruct(this_object());
}

// Called after create() - set up drops
void reset() {
    // Only set up drops if we don't have inventory yet
    // (prevents re-creating drops on room reset)
    if (sizeof(all_inventory(this_object())) == 0) {
        setup_drops();
    }
}
