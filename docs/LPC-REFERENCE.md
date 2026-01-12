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
| `int` | Integer (64-bit signed) | `42`, `-17`, `0` |
| `string` | Text string | `"hello"`, `"world\n"` |
| `object` | Reference to an LPC object | `this_object()` |
| `mapping` | Key-value dictionary | `([ "a": 1, "b": 2 ])` |
| `mixed` | Any type | Used for generic functions |
| `void` | No value (function returns) | `void setup() { }` |

**Note on integers:** LPC integers are 64-bit signed, supporting values from -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807. This matches modern LDMud/FluffOS behavior and allows for large values like experience points, gold, and timestamps without overflow concerns.

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

### Operator Precedence

Operators listed from **highest precedence** (evaluated first) to **lowest**. Based on classic LDMud precedence rules.

| Precedence | Operators | Description | Associativity |
|------------|-----------|-------------|---------------|
| 1 (highest) | `[]` `->` `::` | Indexing, arrow call, scope | Left to right |
| 2 | `++` `--` (postfix) | Postfix increment/decrement | Left to right |
| 3 | `++` `--` `-` `!` `~` (prefix) | Prefix unary operators | Right to left |
| 4 | `*` `/` `%` | Multiplicative | Left to right |
| 5 | `+` `-` | Additive | Left to right |
| 6 | `<<` `>>` | Bitwise shifts | Left to right |
| 7 | `<` `<=` `>` `>=` | Relational | Left to right |
| 8 | `==` `!=` | Equality | Left to right |
| 9 | `&` | Bitwise AND | Left to right |
| 10 | `^` | Bitwise XOR | Left to right |
| 11 | `\|` | Bitwise OR | Left to right |
| 12 | `&&` | Logical AND | Left to right |
| 13 | `\|\|` | Logical OR | Left to right |
| 14 | `?:` | Ternary conditional | Right to left |
| 15 | `=` `+=` `-=` `*=` `/=` etc. | Assignment | Right to left |
| 16 (lowest) | `,` | Comma (sequence) | Left to right |

**Important:** Unlike some modern languages, bitwise operators (`&`, `|`, `^`) have *lower* precedence than comparison operators. This means:

```c
// Bitwise AND after comparison - may surprise you!
x & 1 == 0     // Parsed as: x & (1 == 0), NOT (x & 1) == 0
x & 1 == 0     // Use parentheses: (x & 1) == 0

// Standard arithmetic precedence
5 + 3 * 2      // 11, not 16

// Logical AND before OR
a || b && c    // a || (b && c)

// Ternary is right-associative
a ? b : c ? d : e    // a ? b : (c ? d : e)
```

*Reference: [LDMud operator documentation](http://abathur.github.io/ldmud-doc/build/html/syntax/operators.html)*

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

## Error Handling

### catch/throw

LPC provides `catch()` and `throw()` for error handling.

```c
// catch() evaluates an expression and catches errors
// Returns 0 on success, or error string/value if error occurred
mixed err = catch(risky_operation());
if (err) {
    write("Error: " + err + "\n");
}

// throw() raises an error that can be caught by catch()
void validate(int value) {
    if (value < 0) {
        throw("Value must be non-negative");
    }
}

// Catching throw errors
mixed err = catch(validate(-5));  // err = "Value must be non-negative"

// Uncaught throw() propagates as a runtime error
throw("Fatal error");  // Stops execution if not caught
```

**Key behaviors:**
- `catch(expr)` returns `0` if `expr` succeeds (the expression result is discarded)
- `catch(expr)` returns the thrown value/error message if an error occurs
- `throw(value)` raises an error that propagates up the call stack
- Uncaught `throw()` becomes a runtime error with stack trace
- Runtime errors (undefined functions, division by zero, etc.) can also be caught

```c
// Catching runtime errors
mixed err = catch(unknown_func());
// err now contains error message with file:line info

// Nested catch - only innermost catches
mixed outer = catch(({
    mixed inner = catch(throw("inner"));
    // inner == "inner", execution continues here
    throw("outer");  // This throw is caught by outer catch
}));
// outer == "outer"
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

### Function Visibility Modifiers

Visibility modifiers control how functions can be accessed. These follow authentic LPC/LDMud semantics.

| Modifier | call_other/`->` | Inherited? | Use Case |
|----------|-----------------|------------|----------|
| (none/public) | Yes | Yes | Public API for other objects |
| `static` | **No** | Yes | Internal helper shared with children |
| `private` | No | **No** | Implementation detail, not shared |
| `protected` | No | Yes | Protected API for children only |
| `nomask` | Yes | Yes | Cannot be overridden |

**Important:** LPC `static` is different from C++! In LPC, `static` means "not callable externally" but still inherits to children.

#### When to Use Each Modifier

**public (default)** - Use for functions that other objects need to call:
```c
// /std/player.c
// Other objects call player->tell() to send messages
void tell(string msg) {
    write(msg);
}

// Commands call player->set_hp() to modify health
void set_hp(int hp) {
    this_player()->hp = hp;
}
```

**static** - Use for internal helpers that children should inherit but external code shouldn't call:
```c
// /std/living.c
// Internal combat calculation - children inherit it, but not call_other accessible
static int calculate_damage(int base, int modifier) {
    return base * modifier / 100;
}

void attack(object target) {
    int dmg = calculate_damage(strength, weapon_bonus);  // Internal call works
    target->receive_damage(dmg);
}

// /std/monster.c
inherit "/std/living";
// Monster can call calculate_damage() because static is inherited
void special_attack(object target) {
    int dmg = calculate_damage(strength * 2, 150);
    target->receive_damage(dmg);
}
```

**private** - Use for implementation details that shouldn't be inherited at all:
```c
// /std/container.c
// Implementation detail - only this file should know about it
private void recalculate_weight() {
    total_weight = 0;
    foreach(object o in contents) {
        total_weight += o->query_weight();
    }
}

void add_item(object item) {
    contents += ({ item });
    recalculate_weight();  // Internal call works
}

// /obj/chest.c
inherit "/std/container";
// recalculate_weight() is NOT available here - it's private
// If you try to call it, the call fails
```

**protected** - Use for functions children can override but external code shouldn't call:
```c
// /std/monster.c
// Subclasses override this, but players can't call monster->get_loot_table()
protected mixed *get_loot_table() {
    return ({ "/obj/gold", "/obj/gem" });
}

void die() {
    foreach(string path in get_loot_table()) {
        clone_object(path)->move(environment());
    }
    destruct(this_object());
}

// /world/dragon.c
inherit "/std/monster";
// Override the protected function with dragon-specific loot
protected mixed *get_loot_table() {
    return ({ "/obj/dragon_scale", "/obj/treasure_chest" });
}
```

**nomask** - Use for functions that must not be overridden (security-critical):
```c
// /std/player.c
// Security: don't let wizard code override this to cheat
nomask int query_level() {
    return level;
}

// Inherited objects cannot override query_level()
```

#### Security Note

The visibility check only applies to external calls via `call_other()` and the arrow operator (`->`). Code executing within the same object or inheritance chain can always call any function directly.

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

## Preprocessor

LPC files are processed by a C-style preprocessor before compilation. The preprocessor handles file inclusion, macro definitions, and conditional compilation.

### #include

Include another file's contents:

```c
#include "/std/living.h"      // Absolute path (from mudlib root)
#include <std/types.h>        // Also absolute path
#include "local_defs.c"       // Relative to current file
```

The `.c` extension is added automatically if not present. Circular includes are detected and skipped.

### #define / #undef

Define and undefine macros:

```c
#define MAX_HP 100
#define DEBUG                  // Value defaults to 1
#define MESSAGE "Hello World"

int health = MAX_HP;           // Becomes: int health = 100;

#undef DEBUG                   // Remove the DEBUG macro
```

Macros are simple text substitution with word boundary matching (won't replace partial words).

### #ifdef / #ifndef / #else / #endif

Conditional compilation based on whether a macro is defined:

```c
#define DEBUG

#ifdef DEBUG
    write("Debug mode enabled\n");
#else
    // Production code
#endif

#ifndef RELEASE
    // Include extra logging
#endif
```

Conditions can be nested:

```c
#ifdef FEATURE_A
    #ifdef FEATURE_B
        // Both features enabled
    #endif
#endif
```

### Predefined Macros

You can define macros from C# code before loading objects:

```csharp
preprocessor.Define("MUDLIB_VERSION", "1.0");
preprocessor.Define("DEBUG");  // Defaults to "1"
```

### Line Number Preservation

The preprocessor outputs empty lines for directives to preserve line numbers, ensuring error messages reference the correct source line.

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
| `users()` | Get array of all connected player objects |
| `linkdead_users()` | Get array of linkdead player objects |
| `query_linkdead(obj)` | Returns 1 if object is linkdead, 0 otherwise |

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
| `find_call_out(func)` | Get time until callout fires |

### Shadows

Shadows allow one object to intercept function calls to another object.

| Efun | Description |
|------|-------------|
| `shadow(ob)` | Make this_object() shadow `ob`, returns 1 on success |
| `query_shadowing(ob)` | Returns the object shadowing `ob`, or 0 |
| `unshadow()` | Remove this_object()'s shadow from its target |

**How shadows work:**
- When `shadow(ob)` succeeds, function calls to `ob` are intercepted by the shadow
- If the shadow defines a function, it handles the call
- If not, the call passes through to the original object
- Objects can define `query_prevent_shadow()` returning 1 to prevent being shadowed

```c
// invisibility_shadow.c
void create() {
    shadow(find_player("bob"));  // Shadow player "bob"
}

string query_name() {
    // Intercept query_name calls
    return "Someone Invisible";
}

int query_prevent_shadow() {
    return 1;  // Prevent this shadow from being shadowed
}
```

### Object Persistence

| Efun | Description |
|------|-------------|
| `save_object(path)` | Save this_object()'s variables to a file (.o extension added if missing) |
| `restore_object(path)` | Restore this_object()'s variables from a file |

**Notes:**
- Saves int, string, float, arrays, and mappings of simple types
- Object references cannot be saved (skipped silently)
- Files are stored in LPC save format (varname value pairs)

### Server Control (Admin Only)

| Efun | Description |
|------|-------------|
| `shutdown()` | Initiate graceful server shutdown |

### Error Handling

| Efun | Description |
|------|-------------|
| `throw(value)` | Raise an error that can be caught by `catch()` |

**Note:** `catch(expr)` is a language construct, not an efun. See [Error Handling](#error-handling) section above.

### Logging

| Efun | Description |
|------|-------------|
| `syslog(message)` | Log a message to the system log (info level) |
| `syslog(level, message)` | Log a message with specified level |

**Log levels:** `"debug"`, `"info"`, `"warning"`, `"error"`

```c
// Simple logging (info level)
syslog("Player entered room");

// Log with explicit level
syslog("debug", "Loading object: " + path);
syslog("warning", "Combat function called with invalid target");
syslog("error", "Critical error: database connection failed");
```

**Server configuration:**
- `--log-level <level>` - Set minimum log level (debug/info/warning/error)
- `--log-file <path>` - Also log to a file

### Action System

The action system allows objects to register custom command handlers via `add_action()`.

| Efun | Description |
|------|-------------|
| `add_action(func, verb, [flags])` | Register function to handle a verb |
| `query_verb()` | Get the current command verb being processed |
| `notify_fail(msg)` | Set failure message if command not handled |
| `enable_commands()` | Allow this object to receive commands |
| `disable_commands()` | Disable command receiving |
| `command(str)` | Execute a command as this_player() |

**Flags for add_action:**
- `0` - Exact match (default)
- `1` - Prefix match ("l" matches "look")
- `2` - Allow overriding core commands

**Example:**
```c
void init() {
    add_action("do_pull", "pull");
}

int do_pull(string arg) {
    if (arg != "lever") {
        notify_fail("Pull what?\n");
        return 0;
    }
    write("You pull the lever. A trapdoor opens!\n");
    return 1;
}
```

### Arrays

| Efun | Description |
|------|-------------|
| `sizeof(x)` | Length of array/mapping/string |
| `member_array(elem, arr)` | Find index of element in array (-1 if not found) |
| `allocate(n)` | Create array of n elements (initialized to 0) |
| `copy(x)` | Deep copy an array or mapping |
| `sort_array(arr, dir)` | Sort array (1=ascending, -1=descending) |
| `filter_array(arr, func)` | Filter array elements with callback |
| `map_array(arr, func)` | Transform array elements with callback |

### Mappings

| Efun | Description |
|------|-------------|
| `m_indices(map)` / `keys(map)` | Get array of mapping keys |
| `m_values(map)` / `values(map)` | Get array of mapping values |
| `m_delete(map, key)` | Remove a key from mapping |
| `mkmapping(keys, values)` | Create mapping from two arrays |

### Other

| Efun | Description |
|------|-------------|
| `random(n)` | Random integer from 0 to n-1 |
| `time()` | Current Unix timestamp |
| `typeof(x)` | Get type name as string |

### Strings

| Efun | Description |
|------|-------------|
| `strlen(str)` | Length of string |
| `capitalize(str)` | Capitalize first letter |
| `lower_case(str)` | Convert to lowercase |
| `upper_case(str)` | Convert to uppercase |
| `sprintf(fmt, args...)` | Formatted string (see below) |
| `explode(str, delim)` | Split string into array |
| `implode(arr, delim)` | Join array into string |
| `replace_string(str, from, to)` | Replace all occurrences of `from` with `to` |
| `trim(str)` | Remove leading/trailing whitespace |
| `strsrch(str, substr)` | Find position of substring (-1 if not found) |
| `sscanf(str, fmt, vars...)` | Parse formatted string into variables (see below) |

**sprintf format specifiers:**
- `%s` - String
- `%d`, `%i` - Decimal integer
- `%o` - Octal
- `%x` - Hexadecimal (lowercase)
- `%X` - Hexadecimal (uppercase)
- `%O` - Object/value dump
- `%%` - Literal percent sign
- Width: `%5d` (min width 5), `%-5s` (left-aligned), `%05d` (zero-padded)

```c
sprintf("Name: %-10s HP: %d/%d", name, hp, max_hp);
// "Name: Orc       HP: 45/100"
```

**sscanf format specifiers:**
- `%s` - String (until next literal or end)
- `%d` - Integer
- `%*s`, `%*d` - Skip (don't assign)
- Returns number of variables assigned

```c
string who;
int damage;
sscanf("Bob attacks for 15 damage", "%s attacks for %d damage", who, damage);
// who = "Bob", damage = 15, returns 2
```

### Type Checking

| Efun | Description |
|------|-------------|
| `intp(x)` | Is x an integer? |
| `stringp(x)` | Is x a string? |
| `objectp(x)` | Is x an object? |
| `pointerp(x)` / `arrayp(x)` | Is x an array? |
| `mappingp(x)` | Is x a mapping? |
| `clonep(x)` | Is x a clone (not a blueprint)? |

### Testing

| Efun | Description |
|------|-------------|
| `assert(cond, msg)` | Assert condition is true (for tests) |

### Command Aliases

Players have personal command aliases that expand short commands to longer ones (e.g., "n" expands to "go north"). These efuns allow LPC code to manage aliases.

| Efun | Description |
|------|-------------|
| `query_aliases()` | Get all aliases as a mapping (alias -> command) |
| `query_alias(name)` | Get specific alias definition, or 0 if not found |
| `set_alias(name, cmd)` | Set or update an alias (returns 1 on success) |
| `remove_alias(name)` | Remove an alias (returns 1 on success) |
| `reset_aliases()` | Reset all aliases to defaults (returns 1 on success) |

**Note:** Protected commands (quit, alias, password, etc.) cannot be aliased for security reasons.

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
