# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## CRITICAL: Git Commit Rules

**NEVER add Co-Authored-By trailers to commits. NEVER. The user is the sole author of all commits in this repository. This is non-negotiable.**

## CRITICAL: Development Workflow

**Commit after every major change.** Do not batch unrelated features together. Each feature should be a complete unit:

1. Implement the feature
2. Add appropriate tests for the new functionality
3. Run `dotnet test` and ensure all tests pass
4. Update documentation in `docs/` as needed
5. Commit with a descriptive message
6. Move on to the next task

## Project Overview

LPMud Revival is a modern reimplementation of the classic LPMud architecture in C#/.NET 9. The project follows a two-layer architecture:

- **Driver** (C#): Engine handling networking, object management, and LPC interpreter
- **Mudlib** (LPC): Game content including rooms, items, NPCs, commands, combat

## Build and Run Commands

```bash
# Build the project
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

## Documentation

All user-facing documentation lives in the `docs/` directory:

| Document | Contents |
|----------|----------|
| `docs/LPC-REFERENCE.md` | LPC language reference: syntax, types, operators, control flow, efuns |
| `docs/OBJECT_MODEL.md` | Blueprint/clone architecture, inheritance, lifecycle hooks, variable storage |
| `docs/ARCHITECTURE.md` | High-level system architecture |
| `docs/IMPLEMENTATION.md` | 10-milestone implementation plan and status |

When you need to look up LPC syntax, efun signatures, or object model details, read the appropriate doc file.

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

## Testing Strategy

- **Unit tests** (xUnit): `LexerTests.cs`, `ParserTests.cs`, `InterpreterTests.cs`, `ObjectManagerTests.cs`
- **Integration tests**: Programmatic connection testing
- **LPC tests**: Files in `/lpc-tests/` with `run_tests()` function using `assert()` efun

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
