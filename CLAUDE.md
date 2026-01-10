# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## CRITICAL: Git Commit Rules

**NEVER add Co-Authored-By trailers to commits. NEVER. The user is the sole author of all commits in this repository. This is non-negotiable.**

## Project Overview

LPMud Revival is a modern reimplementation of the classic LPMud architecture in C#/.NET 9. The project follows a two-layer architecture:

- **Driver** (C#): Engine handling networking, object management, and LPC interpreter
- **Mudlib** (LPC): Game content including rooms, items, NPCs, commands, combat

## Build and Run Commands

```bash
# Build the project (once src/Driver exists)
dotnet build src/Driver/Driver

# Run tests
dotnet test

# Server mode
dotnet run --project src/Driver/Driver -- --mudlib ./mudlib --port 4000

# Interactive REPL
dotnet run --project src/Driver/Driver -- --repl

# Evaluate single expression
dotnet run --project src/Driver/Driver -- --eval "5 + 3 * 2"

# Run LPC file with specific function
dotnet run --project src/Driver/Driver -- --mudlib ./mudlib --run test.c --call test_func

# Run LPC test suite
dotnet run --project src/Driver/Driver -- --mudlib ./mudlib --test ./lpc-tests/
```

## Architecture

### Driver Components (C#)

- **Network Layer** (`TelnetServer.cs`, `Connection.cs`): TCP/telnet handling, line-based input buffering
- **Object Manager** (`ObjectManager.cs`): Blueprints vs clones, lifecycle (`create()` → `init()` → `dest()`)
- **LPC Interpreter**: Hand-written lexer → recursive descent parser → tree-walking evaluator
- **Game Loop** (`GameLoop.cs`): Heartbeats (periodic callbacks) and callouts (delayed calls)
- **Efun Library**: C# implementations of functions callable from LPC code

### Mudlib Structure (LPC)

| Directory | Purpose |
|-----------|---------|
| `/std/` | **Core lib** - Base classes (don't modify): `object.c`, `room.c`, `player.c`, `weapon.c` |
| `/cmds/std/` | Standard commands available to all players |
| `/cmds/player/` | Player-only commands (future) |
| `/cmds/wizard/` | Wizard commands (future) |
| `/world/rooms/` | Reference world rooms organized by area (town/, castle/, wilderness/) |
| `/world/items/` | Reference world items (weapons/, armor/, misc/) |
| `/world/mobs/` | Reference world NPCs/monsters (future) |
| `/wizards/` | Wizard home directories (future) |
| `/secure/` | Security-critical code (future): `master.c`, `simul_efun.c` |

### Inheritance Hierarchy

```
object.c → room.c
         → living.c → player.c
                    → monster.c
         → container.c
```

### Key LPC Concepts

- **Blueprint vs Clone**: Blueprint is the compiled LPC file (singleton); clone is an instance with its own state
- **Efuns**: External functions provided by the driver (e.g., `clone_object()`, `tell_room()`, `this_player()`)
- **Heartbeats**: Objects call `set_heart_beat(1)` to receive periodic `heart_beat()` calls (used for combat, AI, regeneration)
- **Callouts**: `call_out("func", delay)` schedules delayed function calls

## Implementation Milestones

The project follows a 10-milestone phased approach (see docs/IMPLEMENTATION.md):

1. Lexer → 2. Parser → 3. Interpreter + REPL → 4. Functions & Variables → 5. Object Model → 6. Telnet Server → 7. Player & Commands → 8. Rooms & Movement → 9. Heartbeats & Callouts → 10. Combat

## Testing Strategy

- **Unit tests** (xUnit): `LexerTests.cs`, `ParserTests.cs`, `InterpreterTests.cs`, `ObjectManagerTests.cs`
- **Integration tests**: Programmatic connection testing
- **LPC tests**: Files in `/lpc-tests/` with `run_tests()` function using `assert()` efun

## LPC Language Notes

- C-like syntax with `inherit` for single inheritance
- Types: `int` (64-bit), `string`, `object`, `mapping`, `mixed`, `void`
- Array syntax: `({ 1, 2, 3 })`
- Mapping syntax: `([ "key": value ])`
- Call parent function with `::`
- Lifecycle hooks: `create()`, `init()`, `dest()`
- LPC integers are 64-bit to support large values (XP, gold, etc.)

## Available Efuns

### Object Management
- `clone_object(path)` - Create a clone from a blueprint
- `load_object(path)` - Load/get a blueprint object
- `find_object(name)` - Find object by full name
- `destruct(obj)` - Destroy an object
- `this_object()` - Get the current executing object
- `this_player()` - Get the current interactive player
- `object_name(obj)` - Get full object name
- `file_name(obj)` - Get object's file path
- `previous_object(n)` - Get nth caller from call stack

### Movement & Environment
- `move_object(dest)` - Move this_object() to destination
- `environment(obj)` - Get object's container
- `all_inventory(obj)` - Get array of contents
- `first_inventory(obj)` - Get first item in contents
- `next_inventory(obj)` - Get next sibling in parent's contents
- `present(str, obj)` - Find object by id in environment

### Communication
- `write(str)` - Write to current player
- `tell_object(obj, str)` - Send message to object
- `tell_room(room, str, exclude)` - Broadcast to room
- `say(msg)` - Broadcast to room except this_player()

### Living/Interactive
- `set_living(flag)` - Mark object as living
- `living(obj)` - Test if object is living
- `set_living_name(name)` - Set living name for lookups
- `query_living_name(obj)` - Get living name
- `find_living(name)` - Find object by living name
- `find_player(name)` - Find connected player by name
- `interactive(obj)` - Test if object is a player connection
- `users()` - Get array of all connected players

### Heartbeats & Callouts
- `set_heart_beat(flag)` - Enable/disable heartbeat
- `query_heart_beat(obj)` - Check heartbeat status
- `call_out(func, delay, args...)` - Schedule delayed call
- `remove_call_out(func_or_id)` - Cancel a callout
- `find_call_out(func)` - Get time until callout fires

### Input Handling
- `input_to(func, [flags])` - Capture next line of input

### Action System (add_action)
- `add_action(func, verb, [flags])` - Register command handler for a verb
- `query_verb()` - Get current command verb being processed
- `notify_fail(msg)` - Set failure message if no handler succeeds
- `enable_commands()` - Enable command processing for this object
- `disable_commands()` - Disable command processing
- `command(str)` - Execute command as this_player()

### Type Predicates
- `intp(x)` - Returns 1 if x is an integer
- `stringp(x)` - Returns 1 if x is a string
- `objectp(x)` - Returns 1 if x is an object
- `pointerp(x)` / `arrayp(x)` - Returns 1 if x is an array
- `mappingp(x)` - Returns 1 if x is a mapping
- `clonep(obj)` - Returns 1 if obj is a clone (not blueprint)

### Strings
- `strlen(str)` - String length
- `sprintf(fmt, args...)` - Formatted string
- `explode(str, delim)` - Split string to array
- `implode(arr, delim)` - Join array to string
- `lower_case(str)` - Convert to lowercase
- `upper_case(str)` - Convert to uppercase
- `capitalize(str)` - Capitalize first letter
- `strsrch(str, substr, [start])` - Find substring position
- `sscanf(str, fmt, vars...)` - Parse formatted input
- `replace_string(str, from, to)` - Replace all occurrences
- `trim(str)` - Remove leading/trailing whitespace

### Arrays
- `sizeof(arr)` - Array/mapping size
- `member_array(elem, arr)` - Find element index
- `sort_array(arr, dir)` - Sort array (1=asc, -1=desc)
- `unique_array(arr)` - Remove duplicates
- `filter_array(arr, func)` - Filter with callback
- `map_array(arr, func)` - Transform with callback
- `allocate(n)` - Create array of n elements (all 0)
- `copy(x)` - Deep copy array or mapping

### Mappings
- `m_indices(map)` / `keys(map)` - Get all keys
- `m_values(map)` / `values(map)` - Get all values
- `m_delete(map, key)` - Remove key from mapping
- `mkmapping(keys, values)` - Create mapping from arrays

### Time
- `time()` - Unix timestamp
- `ctime(t)` - Human-readable time string

### Type Conversion
- `typeof(x)` - Get type name
- `to_string(x)` - Convert to string
- `to_int(x)` - Convert to integer

### File I/O
- `read_file(path, [start], [lines])` - Read file contents
- `write_file(path, text, [flag])` - Write/append to file
- `file_size(path)` - Get file size (-1 if not found)
- `rm(path)` - Delete a file

### Persistence
- `save_object(path)` - Save variables to file
- `restore_object(path)` - Restore variables from file

### Hot-Reload
- `update(path)` - Reload object and dependents
- `inherits(path)` - Get inherited object paths

### Other
- `random(n)` - Random number 0 to n-1
- `call_other(obj, func, args...)` - Call function on another object

## Development Guidelines

### Transparency

Your thinking must be obvious to the user at all times:
- **Never silently skip past errors or failures** - always explain what happened and why
- **If you decide something is acceptable** (e.g., a shell issue vs a code bug), explain your reasoning explicitly
- **If you work around a problem**, say so and explain why
- **If a test fails or a command errors**, investigate and explain before moving on
- The user should never have to ask "why did you ignore that?" - preempt with clear explanations

### Commit Messages

Use descriptive commit messages that explain what changed and why:
- Start with a short summary line (imperative mood)
- Add bullet points for specific changes if needed
- Keep authentic to classic LPMud/LDMud behavior when implementing language features
- **NEVER add Co-Authored-By trailers** (see CRITICAL section at top of this file)
