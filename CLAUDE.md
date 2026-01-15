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

# Run C# unit tests (537 tests)
dotnet test

# Server mode
dotnet run --project src/Driver/Driver -- --server --mudlib ./mudlib --port 4000

# Interactive REPL (basic expression evaluation)
dotnet run --project src/Driver/Driver -- --repl

# Evaluate single expression
dotnet run --project src/Driver/Driver -- --eval "5 + 3 * 2"

# Tokenize LPC file (check syntax)
dotnet run --project src/Driver/Driver -- --tokenize mudlib/cmds/std/look.c
```

## Testing

### C# Unit Tests
```bash
dotnet test
```
This runs 537+ xUnit tests covering lexer, parser, interpreter, and object management.

### LPC Syntax Validation
```bash
# Tokenize a file to check for syntax errors
dotnet run --project src/Driver/Driver -- --tokenize <file.c>
```

### Integration Testing
Start the server and manually test commands via telnet:
```bash
# Terminal 1: Start server
dotnet run --project src/Driver/Driver -- --server --mudlib ./mudlib --port 4000

# Terminal 2: Connect and test
telnet localhost 4000
```

### Testing New LPC Code
When adding new LPC files, always:
1. Run `dotnet test` to ensure C# tests pass
2. Tokenize the new file: `dotnet run ... -- --tokenize <file.c>`
3. Start server and manually test the new functionality
4. Check server logs for runtime errors

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

- **C# Unit tests** (xUnit): `LexerTests.cs`, `ParserTests.cs`, `InterpreterTests.cs`, `ObjectManagerTests.cs`
  - Run with: `dotnet test`
- **LPC syntax validation**: Tokenize files to catch syntax errors
  - Run with: `dotnet run --project src/Driver/Driver -- --tokenize <file.c>`
- **Integration tests**: Start server and test commands manually via telnet
- **LPC test files**: Files in `/lpc-tests/` can be loaded to test specific functionality

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
