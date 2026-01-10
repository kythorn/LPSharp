# LPMud Revival

A modern reimplementation of the classic LPMud architecture for educational purposes. This project recreates the magic of LPMuds—text-based multiplayer worlds with live-scriptable content—using modern technology while staying true to the original design philosophy.

## What is an LPMud?

LPMuds were text-based multiplayer games that pioneered ideas decades ahead of their time:
- **Hot code reloading**: Update game content while players are connected
- **Object-oriented world building**: Everything is an object that can inherit, be cloned, and interact
- **Separation of concerns**: The driver (engine) is completely separate from the mudlib (game content)
- **Scriptable content**: World builders write game logic in LPC, a C-like scripting language

## Architecture

This project follows the classic two-layer architecture:

| Layer | Language | Purpose |
|-------|----------|---------|
| **Driver** | C# / .NET 9 | Engine: networking, object management, LPC interpreter |
| **Mudlib** | LPC (custom) | Game content: rooms, items, NPCs, commands, combat |

The driver exposes a set of **efuns** (external functions) that LPC code can call. This provides natural sandboxing—scripts can only do what the driver explicitly allows.

## Project Status

**Phase**: Milestone 9 Complete - Heartbeats & Callouts

| Milestone | Status |
|-----------|--------|
| 1. Lexer | Complete |
| 2. Parser | Complete |
| 3. Interpreter + REPL | Complete |
| 4. Functions & Variables | Complete |
| 5. Object Model | Complete |
| 6. Telnet Server | Complete |
| 7. Player & Commands | Complete |
| 8. Rooms & Movement | Complete |
| 9. Heartbeats & Callouts | Complete |
| 10. Combat | Not Started |

See [docs/IMPLEMENTATION.md](docs/IMPLEMENTATION.md) for the full implementation plan.

## Quick Start

```bash
# Server mode
dotnet run --project src/Driver/Driver -- --mudlib ./mudlib --port 4000

# Interactive REPL
dotnet run --project src/Driver/Driver -- --repl

# Evaluate expression
dotnet run --project src/Driver/Driver -- --eval "5 + 3 * 2"

# Run LPC tests
dotnet run --project src/Driver/Driver -- --test ./lpc-tests/
```

## Project Structure

```
/src
  /Driver
    /Driver            # C# driver implementation
    /Driver.Tests      # Unit and integration tests (420+ tests)
/mudlib                # Game content (LPC)
  /std                 # Base classes: object.c, room.c, player.c, weapon.c
  /cmds/std            # Standard commands: say, look, quit, go, directions
  /cmds/admin          # Admin commands: update
  /world/rooms         # Game world organized by area (town, castle, wilderness)
  /world/items         # Items: weapons, armor, misc
/test-mudlib           # Minimal mudlib for automated testing
/lpc-tests             # LPC test scripts using assert() efun
/docs                  # Documentation
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md) - System design and component overview
- [Implementation Plan](docs/IMPLEMENTATION.md) - Phased development milestones
- [LPC Reference](docs/LPC-REFERENCE.md) - Language specification and efun documentation

## Design Principles

1. **Educational focus**: Code should be readable and understandable, not just functional
2. **Authentic experience**: Recreate the LPC scripting feel, not just the game mechanics
3. **Incremental development**: Each milestone produces something runnable and testable
4. **Test-driven**: Unit tests, integration tests, and in-language LPC tests

## Key Features

- **Hand-written parser**: Recursive descent, fully readable and debuggable
- **Classic persistence model**: Player stats persist, inventory does not (old-school LPMud style)
- **Combat-focused**: Primary game system to exercise all components
- **Multiple CLI modes**: Server, REPL, eval, script runner, test runner

## Future Considerations

- **Domains system**: Organize mudlib into areas owned by different builders
- **Multiple inheritance**: Currently single inheritance; architecture supports adding this later
- **WebSocket support**: Browser-based clients alongside traditional telnet

## License

*TBD*

## Acknowledgments

Inspired by the LPMud family: LPMud 2.4.5, MudOS, DGD, FluffOS, and all the MUDs built on them.
