// /std/monster.c
// Base class for NPC monsters

inherit "/std/living";

string monster_name;
int aggressive;
int xp_value;

void create() {
    ::create();
    set_short("a monster");
    monster_name = "monster";
    aggressive = 0;
    xp_value = 10;
}

string query_name() {
    return monster_name;
}

void set_name(string n) {
    monster_name = n;
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

    // If aggressive, attack any player that enters
    if (aggressive) {
        player = this_player();
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
