# Object Model Architecture

This document describes the blueprint/clone architecture used in the LPMud Revival project, based on classic LPMud/LDMud design.

## Overview

LPC uses a **prototype-based object system** where objects are either **blueprints** (master objects) or **clones** (instances). This differs from class-based OOP languages like Java or C#.

## Blueprints vs Clones

### Blueprints (Master Objects)

A blueprint is a **singleton** representing the compiled LPC file:

- **One per file path**: Only one instance of `/std/room.c` exists in memory
- **Contains code**: Functions and their implementations
- **Cached forever**: Once loaded, stays in memory (until explicitly destructed)
- **Named by path**: `/std/room`, `/obj/weapons/sword`, etc. (no `.c` extension)
- **Can have variables**: But they're shared across all references (rarely used except for daemons)
- **Loaded implicitly**: When first referenced, inherited, or explicitly via `load_object()`

**When to use:**
- Base classes (`/std/object.c`, `/std/room.c`, `/std/weapon.c`)
- Singleton daemons (combat daemon, login manager)
- Rooms (one instance per location)
- Unique artifacts (Excalibur - only one in the game)

### Clones

A clone is an **instance** created from a blueprint:

- **Multiple per blueprint**: Many `/obj/weapons/plain_sword` clones can exist
- **Own variable storage**: Each clone has independent state
- **References blueprint**: Shares code (functions), not state (variables)
- **Named with suffix**: `/obj/weapons/plain_sword#1`, `/obj/weapons/plain_sword#42`, etc.
- **Created explicitly**: Via `clone_object()` efun
- **Independent lifecycle**: Can be destructed individually without affecting other clones

**When to use:**
- Generic items (swords, potions, keys) sold in shops or found as loot
- Monsters (orcs, dragons)
- Players (each connection gets a clone of `/std/player.c`)

## Key Design Decision: Specific Items Have Their Own Files

Unlike some OOP patterns, LPMud does **not** use generic "Weapon" classes that are parameterized:

```c
// ❌ NOT how LPMud works:
Weapon excalibur = new Weapon("Excalibur", 100, "legendary");
Weapon plain_sword = new Weapon("plain sword", 10, "common");

// ✅ How LPMud works:
// Each specific item type has its own file:

// /obj/weapons/excalibur.c
inherit "/std/weapon";
void create() {
    ::create();
    set_name("Excalibur");
    set_short("Excalibur, the legendary blade");
    set_long("An ancient sword of immense power.");
    set_damage(100);
}

// /obj/weapons/plain_sword.c
inherit "/std/weapon";
void create() {
    ::create();
    set_name("plain sword");
    set_short("a plain sword");
    set_long("A cheap and rather dull plain sword.");
    set_damage(10);
}
```

### When to Clone vs When to Load

| Object Type | File Example | Usage | Naming |
|-------------|--------------|-------|--------|
| Generic item | `/obj/weapons/plain_sword.c` | `clone_object()` | `/obj/weapons/plain_sword#1`, `#2`, etc. |
| Unique artifact | `/obj/weapons/excalibur.c` | `load_object()` or direct reference | `/obj/weapons/excalibur` (singleton) |
| Room | `/room/market.c` | Load automatically on reference | `/room/market` (singleton) |
| Monster | `/obj/monsters/orc.c` | `clone_object()` | `/obj/monsters/orc#1`, `#2`, etc. |
| Base class | `/std/weapon.c` | Never instantiated, only inherited | `/std/weapon` (blueprint) |
| Daemon | `/secure/combat_d.c` | Load once, never clone | `/secure/combat_d` (singleton) |

### Shop Example

```c
// /room/blacksmith.c
inherit "/std/room";

void create() {
    ::create();
    set_short("Blacksmith's shop");
    set_long("Weapons line the walls of this smithy.");

    // Create multiple plain swords for sale
    object sword1 = clone_object("/obj/weapons/plain_sword");  // #1
    object sword2 = clone_object("/obj/weapons/plain_sword");  // #2
    object sword3 = clone_object("/obj/weapons/plain_sword");  // #3

    // Each clone is independent
    move_object(sword1, this_object());
    move_object(sword2, this_object());
    move_object(sword3, this_object());
}
```

## Architecture Components

### 1. MudObject (C#)

Represents both blueprints and clones.

```csharp
class MudObject {
    string FilePath;           // "/obj/weapons/sword"
    string ObjectName;         // "/obj/weapons/sword" or "/obj/weapons/sword#5"
    bool IsBlueprint;          // true for blueprints, false for clones
    int? CloneNumber;          // null for blueprints, 1+ for clones

    LpcProgram Program;        // Compiled code (shared by clones)
    Dictionary<string, LpcValue> Variables;  // Instance variables

    MudObject? Blueprint;      // For clones: reference to their blueprint
    List<MudObject> Clones;    // For blueprints: list of active clones (for tracking)
}
```

### 2. LpcProgram (C#)

Represents compiled LPC code (shared across clones).

```csharp
class LpcProgram {
    string FilePath;                            // "/obj/weapons/sword"
    List<LpcProgram> InheritedPrograms;         // Parents in inheritance chain
    Dictionary<string, FunctionDefinition> Functions;
    List<string> VariableNames;                 // Variable declarations
}
```

### 3. ObjectManager (C#)

Central singleton managing all objects.

```csharp
class ObjectManager {
    // Blueprints are cached by file path (without .c extension)
    ConcurrentDictionary<string, MudObject> blueprints;   // path -> blueprint

    // All objects (blueprints + clones) indexed by name
    ConcurrentDictionary<string, MudObject> allObjects;   // name -> object

    // Clone counter per blueprint path
    Dictionary<string, int> cloneCounters;                // path -> next #

    MudObject LoadObject(string path);     // Get/create blueprint
    MudObject CloneObject(string path);    // Create clone instance
    MudObject? FindObject(string name);    // Lookup by name
    void DestructObject(MudObject obj);    // Remove and cleanup
}
```

## Inheritance

LPC supports **single inheritance** via the `inherit` statement:

```c
// /obj/weapons/sword.c
inherit "/std/weapon";

void create() {
    ::create();  // Call parent's create()
    set_name("sword");
    set_damage(10);
}
```

### How Inheritance Works

1. **Compilation**: When `/obj/weapons/sword.c` is compiled:
   - The driver first ensures `/std/weapon.c` is compiled (recursive)
   - The Program for sword contains a reference to weapon's Program

2. **Variable merging**: Variables from both parent and child are stored in the clone

3. **Function resolution**: Child functions override parent functions

4. **:: operator**: Explicitly calls parent version

### Function Resolution

When calling a function on an object:

1. Look in the object's own Program
2. If not found, search InheritedPrograms in order (depth-first)
3. `::func()` explicitly searches parent Programs
4. `/path/to/file::func()` calls specific inherited version (future feature)

## Variable Storage

### Blueprint Variables (Shared State)

Variables in blueprints are **shared** across all references:

```c
// /secure/combat_d.c (singleton daemon)
int total_battles = 0;  // SHARED - increments for everyone

void track_battle() {
    total_battles++;  // All callers see the same value
}
```

**Caution**: Rarely used except for singleton daemons. Most objects should be clones with independent state.

### Clone Variables (Instance State)

Each clone has its own variable storage:

```c
// /obj/weapons/sword.c
int damage;
string owner;

void create() {
    damage = 10;  // Each clone has its own value
}
```

Clone #1's `damage` is completely independent of clone #2's `damage`.

### Inherited Variables

When a clone inherits variables:

```c
// /std/object.c
string short_desc;
int mass;

// /obj/weapons/sword.c
inherit "/std/object";
int damage;
string owner;
```

A clone of `/obj/weapons/sword#1` has variables:
- `short_desc` (from `/std/object`)
- `mass` (from `/std/object`)
- `damage` (from `/obj/weapons/sword`)
- `owner` (from `/obj/weapons/sword`)

All stored in the clone's variable dictionary.

## Lifecycle Hooks

### create()

Called automatically when an object is loaded or cloned:

```c
void create() {
    ::create();  // Always call parent first (good practice)
    set_short("a sharp sword");
    damage = 10;
}
```

**Blueprint**: `create()` called once when first loaded
**Clone**: `create()` called for each new clone

**Order of execution**:
1. Parent's `create()` (via `::create()`)
2. Child's `create()` body

### init()

Called when an object enters another object's environment (Milestone 8 - Rooms & Movement).

### dest()

Called before object is destructed (Milestone 9+ - cleanup).

## Concurrency Considerations

Classic LPMud was single-threaded, but our C# implementation has concurrent network connections.

### Thread-Safe Components

- **ObjectManager.blueprints**: `ConcurrentDictionary` (multiple threads may trigger loads)
- **ObjectManager.allObjects**: `ConcurrentDictionary` (lookups from any thread)
- **Clone counters**: Locked increment to ensure unique IDs

### Single-Threaded Components

- **Object state**: Variables accessed only from game loop thread
- **Function execution**: Interpreter runs on single thread
- **Heartbeats/callouts**: Scheduled on game loop thread

### Loading Strategy

When loading a blueprint:
1. Check cache (thread-safe read)
2. If not found, acquire lock
3. Double-check cache (another thread may have loaded)
4. Compile and add to cache
5. Call `create()` on blueprint

**Note**: For Milestone 5, we'll focus on the basic loading mechanism. Full concurrency handling can be refined as needed.

## Hot-Reloading and Inheritance Chains

### The Challenge

In traditional LPMuds, hot-reloading is **not automatic** for inheritance chains:

> "If object B is recompiled, object A will continue to use the old version of object B until object A is also recompiled."

Example:
```
/std/weapon.c (updated with new damage formula)
  ↓ inherits
/obj/weapons/excalibur.c (still using OLD weapon.c code!)
  ↓ clones
/obj/weapons/excalibur#1 (also using OLD code)
```

### What Traditional LPMuds Do

1. **Tracking**: Use `inherit_list()` to find direct dependencies, `deep_inherit_list()` for full chain
2. **Recursive update**: Commands like `update -R` recursively recompile children
3. **Replace program**: The `replace_program()` efun swaps code in running objects while preserving state
4. **Multiple generations**: It's common during development to have clones of different code versions running simultaneously

### Our Implementation Strategy

**Milestone 5 (now)**:
- ✅ Track inheritance chains (parent → child relationships)
- ✅ Support reloading individual blueprints
- ✅ Build dependency graph (reverse lookup: what inherits from X?)
- ❌ **Don't** implement automatic propagation yet

**Post-Milestone 10 (future)**:
- Implement `update` command to detect file changes
- Implement recursive updates (when `/std/weapon.c` changes, recompile all children)
- Provide mechanisms to update existing clones (or leave them on old code)
- Implement `inherit_list()` and `deep_inherit_list()` efuns

**Reasoning**: Hot-reloading with inheritance chains is complex and requires careful dependency tracking. We'll build the core game loop first, then add sophisticated hot-reload as a polish feature.

## Object Naming Conventions

| Type | Example | Format |
|------|---------|--------|
| Blueprint | `/std/room` | File path without `.c` |
| Clone | `/obj/weapons/sword#1` | File path + `#` + number |
| Inherited | `/std/object` | Referenced in `inherit` statement |

## Efuns for Object Management

| Efun | Description | Milestone |
|------|-------------|-----------|
| `load_object(path)` | Get or load blueprint (singleton) | 5 |
| `clone_object(path)` | Create new clone instance | 5 |
| `this_object()` | Returns current object during execution | 5 |
| `find_object(name)` | Look up object by name | 5 |
| `destruct(object)` | Destroy object and free resources | 5 |
| `this_player()` | Returns player who initiated current action | 7 |
| `environment(object)` | Get object's container | 8 |
| `move_object(dest)` | Move object to new container | 8 |
| `inherit_list(object)` | Get direct inherited programs | Post-10 |
| `deep_inherit_list(object)` | Get full inheritance chain | Post-10 |

## Example Flow

```c
// 1. Load blueprint (implicit during first reference)
//    ObjectManager.LoadObject("/std/weapon")
//    - Compiles /std/weapon.c
//    - Creates blueprint MudObject with IsBlueprint=true
//    - Calls create() once
//    - Caches in blueprints dictionary

// 2. Load child blueprint (inherits parent)
//    ObjectManager.LoadObject("/obj/weapons/sword")
//    - Sees inherit "/std/weapon"
//    - Ensures /std/weapon is loaded (step 1)
//    - Compiles /obj/weapons/sword.c
//    - Links to weapon's Program
//    - Creates blueprint MudObject with IsBlueprint=true
//    - Calls create() once
//    - Caches in blueprints dictionary

// 3. Clone sword
object sword1 = clone_object("/obj/weapons/sword");
//    - Gets blueprint from cache
//    - Creates new MudObject with CloneNumber = 1, IsBlueprint=false
//    - Initializes variables from Program definitions
//    - Calls create() on the clone
//    - Returns /obj/weapons/sword#1

// 4. Clone another sword
object sword2 = clone_object("/obj/weapons/sword");
//    - Gets blueprint from cache (no recompile)
//    - Creates new MudObject with CloneNumber = 2
//    - Calls create() on this clone
//    - Returns /obj/weapons/sword#2

// sword1 and sword2 are independent instances
// Both reference the same blueprint for code
// Each has its own variables (damage, owner, etc.)
```

## Authenticity Notes

This design matches LDMud's architecture:

- **Naming**: Clones use `#` suffix ([LDMud Objects](https://www.ldmud.eu/lpc-objects.html))
- **Blueprint singleton**: "LDMud first looks for an already loaded blueprint" ([LDMud Objects](https://www.ldmud.eu/lpc-objects.html))
- **Inheritance**: "inherit statement allows including all variable declarations and functions" ([LDMud Inheritance](http://www.ldmud.eu/lpc-inheritance.html))
- **:: operator**: Calls original inherited function ([LDMud Inheritance](http://www.ldmud.eu/lpc-inheritance.html))
- **No static/instance distinction**: "not possible to syntactically declare class-only or instance-only" ([LPC on GitHub](https://github.com/burzumishi/LPC))
- **Hot-reload complexity**: "already existing clones will not change just because the master does" ([LPC on GitHub](https://github.com/burzumishi/LPC))
- **Recursive updates**: "You may need to use update -R on objects which inherit the code that you have changed" ([LPMud Efuns](https://www.lysator.liu.se/mud/efuns/index.html))

## References

- [LDMud - LPC Objects](https://www.ldmud.eu/lpc-objects.html)
- [LDMud - LPC Inheritance](http://www.ldmud.eu/lpc-inheritance.html)
- [LDMud GitHub Repository](https://github.com/ldmud/ldmud)
- [LPC Programming Language on GitHub](https://github.com/burzumishi/LPC)
- [FluffOS replace_program()](https://www.fluffos.info/efun/system/replace_program.html)
- [LPMud Efuns Documentation](https://www.lysator.liu.se/mud/efuns/index.html)
- [Lima Mudlib Structure](https://github.com/quixadhal/lima/blob/master/lib/include/mudlib.h)
- [Nightmare Mudlib](https://en-academic.com/dic.nsf/enwiki/453601)
- [Genesis LPMud Tutorial](http://genesisquests.pbworks.com/w/page/41202465/Tutorial%20Journal)
