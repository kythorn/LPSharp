# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
| `/std/` | Base classes: `object.c`, `room.c`, `living.c`, `player.c`, `monster.c` |
| `/obj/` | Clonable items and monsters |
| `/room/` | World areas |
| `/cmds/` | Player commands |

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
- Types: `int`, `string`, `object`, `mapping`, `mixed`, `void`
- Array syntax: `({ 1, 2, 3 })`
- Mapping syntax: `([ "key": value ])`
- Call parent function with `::`
- Lifecycle hooks: `create()`, `init()`, `dest()`

## Development Guidelines

### Commit Messages

Use descriptive commit messages that explain what changed and why:
- Start with a short summary line (imperative mood)
- Add bullet points for specific changes if needed
- Keep authentic to classic LPMud/LDMud behavior when implementing language features
- Do NOT add Co-Authored-By trailers - commits should be attributed to the user only
