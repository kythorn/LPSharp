# LPC Language Reference

This document describes the LPC language as implemented in the LPMud Revival project. LPC is a C-like scripting language designed for building MUD game content.

## Overview

LPC (Lars Pensjö C) is an object-oriented scripting language. Every `.c` file defines an object that can be loaded and cloned by the driver.

```c
// /obj/sword.c
inherit "/std/object";

void create() {
    set_short("a gleaming sword");
    set_long("This is a finely crafted steel sword.");
    set_id("sword");
}
```

## Data Types

### Basic Types

| Type | Description | Example |
|------|-------------|---------|
| `int` | Integer (32-bit signed) | `42`, `-17`, `0` |
| `string` | Text string | `"hello"`, `"world\n"` |
| `object` | Reference to an LPC object | `this_object()` |
| `mapping` | Key-value dictionary | `([ "a": 1, "b": 2 ])` |
| `mixed` | Any type | Used for generic functions |
| `void` | No value (function returns) | `void setup() { }` |

### Arrays

Arrays are ordered lists of values. They can contain mixed types.

```c
// Array literal syntax
int *numbers = ({ 1, 2, 3, 4, 5 });
string *names = ({ "alice", "bob" });
mixed *stuff = ({ 1, "two", this_object() });

// Access by index (0-based)
int first = numbers[0];  // 1

// Array operations
int *combined = ({ 1, 2 }) + ({ 3, 4 });  // ({ 1, 2, 3, 4 })
int *removed = ({ 1, 2, 3 }) - ({ 2 });    // ({ 1, 3 })
int length = sizeof(numbers);              // 5
```

### Mappings

Mappings are key-value dictionaries.

```c
// Mapping literal syntax
mapping exits = ([
    "north": "/room/market",
    "south": "/room/square",
    "east": "/room/shop"
]);

// Access by key
string dest = exits["north"];  // "/room/market"

// Check if key exists
if (exits["west"]) { ... }  // 0 if not present

// Mapping operations
mapping combined = ([ "a": 1 ]) + ([ "b": 2 ]);  // ([ "a": 1, "b": 2 ])
string *keys = m_indices(exits);    // ({ "north", "south", "east" })
mixed *values = m_values(exits);    // ({ "/room/market", ... })
int size = sizeof(exits);           // 3
```

## Variables

### Declaration

```c
// With initialization
int count = 0;
string name = "unknown";
object weapon;  // Defaults to 0

// Multiple declarations
int x, y, z;

// Global variables (outside functions)
int total_score;

void add_score(int points) {
    total_score += points;  // Modifies global
}
```

### Scope

Variables declared inside a function are local to that function. Variables declared outside functions are object-level (global to that object).

```c
int global_var = 10;  // Object-level

void test() {
    int local_var = 5;  // Function-level
    global_var = 20;    // OK - access global
}

void other() {
    // local_var not accessible here
    global_var = 30;    // OK
}
```

## Operators

### Arithmetic

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition | `5 + 3` → `8` |
| `-` | Subtraction | `5 - 3` → `2` |
| `*` | Multiplication | `5 * 3` → `15` |
| `/` | Integer division | `7 / 3` → `2` |
| `%` | Modulo (remainder) | `7 % 3` → `1` |

### Comparison

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equal | `5 == 5` → `1` |
| `!=` | Not equal | `5 != 3` → `1` |
| `<` | Less than | `3 < 5` → `1` |
| `>` | Greater than | `5 > 3` → `1` |
| `<=` | Less or equal | `3 <= 3` → `1` |
| `>=` | Greater or equal | `5 >= 5` → `1` |

### Logical

| Operator | Description | Example |
|----------|-------------|---------|
| `&&` | Logical AND | `1 && 0` → `0` |
| `\|\|` | Logical OR | `1 \|\| 0` → `1` |
| `!` | Logical NOT | `!0` → `1` |

### String

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Concatenation | `"hello" + " " + "world"` → `"hello world"` |

### Assignment

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Assign | `x = 5` |
| `+=` | Add and assign | `x += 3` (same as `x = x + 3`) |
| `-=` | Subtract and assign | `x -= 2` |
| `++` | Increment | `x++` or `++x` |
| `--` | Decrement | `x--` or `--x` |

## Control Flow

### If/Else

```c
if (hp <= 0) {
    die();
} else if (hp < 10) {
    tell_object(this_object(), "You are badly wounded!\n");
} else {
    tell_object(this_object(), "You feel fine.\n");
}
```

### While Loop

```c
int i = 0;
while (i < 10) {
    write(i + "\n");
    i++;
}
```

### For Loop

```c
for (int i = 0; i < 10; i++) {
    write(i + "\n");
}

// Iterate over array
string *names = ({ "alice", "bob", "charlie" });
for (int i = 0; i < sizeof(names); i++) {
    write("Hello, " + names[i] + "!\n");
}
```

### Return

```c
int add(int a, int b) {
    return a + b;
}

void greet(string name) {
    if (!name) {
        return;  // Early exit
    }
    write("Hello, " + name + "!\n");
}
```

## Functions

### Definition

```c
// Basic function
int add(int a, int b) {
    return a + b;
}

// Void function
void greet() {
    write("Hello!\n");
}

// With mixed types
mixed get_value(string key) {
    return data[key];
}
```

### Calling

```c
int result = add(5, 3);
greet();

// Call on another object
object player = find_player("bob");
player->tell("Hello Bob!\n");

// Call function by name
call_other(player, "tell", "Hello!\n");
```

## Objects and Inheritance

### Inherit

Objects can inherit from other objects to reuse code.

```c
// /std/room.c
inherit "/std/object";

mapping exits = ([]);

void set_exit(string dir, string dest) {
    exits[dir] = dest;
}

// /room/square.c
inherit "/std/room";

void create() {
    ::create();  // Call parent's create
    set_short("Town Square");
    set_exit("north", "/room/market");
}
```

### Calling Parent Functions

Use `::` to call the parent's version of a function:

```c
void create() {
    ::create();  // Call parent
    // Then do our own setup
}
```

### Object Lifecycle

| Hook | When Called |
|------|-------------|
| `create()` | Object is loaded or cloned |
| `init()` | Something enters this object (or this object enters something) |
| `dest()` | Object is being destructed |

```c
void create() {
    // Called once when object is created
    hp = 100;
    max_hp = 100;
}

void init() {
    // Called when a player enters this room
    tell_object(this_player(), "Welcome!\n");
}
```

## Comments

```c
// Single-line comment

/*
   Multi-line
   comment
*/

int x = 5;  // Inline comment
```

## Efuns (External Functions)

Efuns are functions provided by the driver, callable from any LPC code.

### Object Management

| Efun | Description |
|------|-------------|
| `this_object()` | Returns the current object |
| `this_player()` | Returns the current player (during command execution) |
| `previous_object()` | Returns the calling object |
| `clone_object(path)` | Create a clone of the object at path |
| `destruct(obj)` | Destroy an object |
| `find_object(path)` | Find a loaded object by path |
| `find_player(name)` | Find a player by name |

### Communication

| Efun | Description |
|------|-------------|
| `write(msg)` | Write to current player (shorthand for tell_object) |
| `tell_object(obj, msg)` | Send message to an object |
| `tell_room(room, msg)` | Send message to all in room |
| `tell_room(room, msg, exclude)` | Send to all except excluded objects |
| `say(msg)` | Send to all in current room except speaker |

### Environment

| Efun | Description |
|------|-------------|
| `environment(obj)` | Get containing object (e.g., player's room) |
| `move_object(dest)` | Move this object to destination |
| `all_inventory(obj)` | Get array of objects inside obj |
| `first_inventory(obj)` | Get first object inside obj |
| `next_inventory(obj)` | Get next sibling in inventory |

### Timing

| Efun | Description |
|------|-------------|
| `set_heart_beat(flag)` | Enable/disable heartbeat for this object |
| `call_out(func, delay, args...)` | Schedule delayed function call |
| `remove_call_out(func)` | Cancel pending callout |

### Data Manipulation

| Efun | Description |
|------|-------------|
| `sizeof(x)` | Length of array/mapping/string |
| `m_indices(map)` | Get array of mapping keys |
| `m_values(map)` | Get array of mapping values |
| `member_array(elem, arr)` | Find index of element in array (-1 if not found) |
| `random(n)` | Random integer from 0 to n-1 |

### Strings

| Efun | Description |
|------|-------------|
| `strlen(str)` | Length of string |
| `capitalize(str)` | Capitalize first letter |
| `lower_case(str)` | Convert to lowercase |
| `sprintf(fmt, args...)` | Formatted string |

### Type Checking

| Efun | Description |
|------|-------------|
| `intp(x)` | Is x an integer? |
| `stringp(x)` | Is x a string? |
| `objectp(x)` | Is x an object? |
| `arrayp(x)` | Is x an array? |
| `mapp(x)` | Is x a mapping? |

### Testing

| Efun | Description |
|------|-------------|
| `assert(cond, msg)` | Assert condition is true (for tests) |

## Example: Complete Object

```c
// /obj/sword.c
// A simple sword that can be wielded for combat

inherit "/std/object";

int damage_bonus;

void create() {
    ::create();
    set_short("a steel sword");
    set_long("This is a well-crafted steel sword with a leather-wrapped hilt.");
    set_id("sword");
    set_id("steel sword");
    damage_bonus = 5;
}

int query_damage_bonus() {
    return damage_bonus;
}

void init() {
    // Add commands when picked up
    add_action("do_wield", "wield");
}

int do_wield(string arg) {
    if (arg != "sword" && arg != "steel sword") {
        return 0;  // Not us
    }

    tell_object(this_player(), "You wield the steel sword.\n");
    tell_room(environment(this_player()),
              this_player()->query_name() + " wields a steel sword.\n",
              ({ this_player() }));
    this_player()->set_weapon(this_object());
    return 1;
}
```

## Example: Room

```c
// /room/tavern.c
// A cozy tavern

inherit "/std/room";

void create() {
    ::create();

    set_short("The Rusty Bucket Tavern");
    set_long(
        "You are in a warm, dimly lit tavern. A fire crackles in the "
        "hearth, and the smell of ale and roasted meat fills the air. "
        "A few locals sit at wooden tables, engaged in quiet conversation."
    );

    set_exit("out", "/room/square");
    set_exit("up", "/room/tavern_upstairs");

    // Spawn the barkeeper
    clone_object("/obj/barkeeper")->move_object(this_object());
}

void init() {
    ::init();
    tell_object(this_player(), "The barkeeper nods in greeting.\n");
}
```

## Example: Monster

```c
// /obj/orc.c
// A basic hostile monster

inherit "/std/monster";

void create() {
    ::create();

    set_short("a menacing orc");
    set_long(
        "This orc is large and muscular, with greenish skin and "
        "yellowed tusks. It eyes you with hostile intent."
    );
    set_id("orc");

    set_hp(50);
    set_max_hp(50);
    set_attack(10);
    set_defense(5);

    set_aggressive(1);  // Will attack players on sight
}

void init() {
    ::init();
    if (this_player() && !is_fighting()) {
        // Attack player who enters
        call_out("start_attack", 1);
    }
}

void start_attack() {
    if (this_player() && environment() == environment(this_player())) {
        tell_room(environment(), "The orc snarls and attacks!\n");
        kill(this_player());
    }
}

void die() {
    tell_room(environment(), "The orc falls to the ground, dead.\n");
    // Respawn after 60 seconds
    call_out("respawn", 60);
    ::die();
}

void respawn() {
    object orc = clone_object("/obj/orc");
    orc->move_object(find_object("/room/cave"));
}
```
