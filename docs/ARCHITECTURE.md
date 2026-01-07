# Architecture

This document describes the system architecture of the LPMud Revival project.

## Overview

The system follows the classic LPMud two-layer architecture, cleanly separating the **driver** (engine) from the **mudlib** (game content).

```
┌─────────────────────────────────────────────────────────────────────┐
│                           DRIVER (C#)                               │
│                                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │
│  │   Network   │  │   Object    │  │     LPC     │  │   Game     │ │
│  │   Layer     │  │   Manager   │  │ Interpreter │  │   Loop     │ │
│  │             │  │             │  │             │  │            │ │
│  │ - Telnet    │  │ - Loading   │  │ - Lexer     │  │ - Heartbeat│ │
│  │ - Sockets   │  │ - Cloning   │  │ - Parser    │  │ - Callouts │ │
│  │ - I/O       │  │ - Lookup    │  │ - Evaluator │  │ - Tick     │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                      Efun Library                            │   │
│  │                                                              │   │
│  │  tell_object()  move_object()  clone_object()  find_player() │   │
│  │  this_object()  this_player()  query_hp()      set_hp()      │   │
│  │  call_out()     heart_beat()   destruct()      sizeof()      │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ Efun calls
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          MUDLIB (LPC)                               │
│                                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │
│  │   /std/     │  │   /obj/     │  │   /room/    │  │  /cmds/    │ │
│  │             │  │             │  │             │  │            │ │
│  │ object.c    │  │ weapon.c    │  │ void.c      │  │ look.c     │ │
│  │ room.c      │  │ armor.c     │  │ town/*.c    │  │ say.c      │ │
│  │ living.c    │  │ potion.c    │  │ dungeon/*.c │  │ go.c       │ │
│  │ player.c    │  │ orc.c       │  │             │  │ attack.c   │ │
│  │ monster.c   │  │ sword.c     │  │             │  │ get.c      │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

## Driver Components

### Network Layer

Handles TCP connections and telnet protocol.

```
┌──────────────────────────────────────────────────┐
│                 TelnetServer                      │
│                                                  │
│  - Listen on configurable port                   │
│  - Accept incoming connections                   │
│  - Create Connection per client                  │
└──────────────────────────────────────────────────┘
                      │
                      │ spawns
                      ▼
┌──────────────────────────────────────────────────┐
│                  Connection                       │
│                                                  │
│  - Socket read/write                             │
│  - Input buffering (line-based)                  │
│  - Output queue                                  │
│  - Link to player object                         │
└──────────────────────────────────────────────────┘
```

**Responsibilities:**
- Accept TCP connections on configured port
- Handle telnet negotiation (or raw mode)
- Buffer input into complete lines
- Route input to player object's command handler
- Queue and send output to clients

### Object Manager

Manages the lifecycle of all LPC objects.

```
┌──────────────────────────────────────────────────────────────────┐
│                        ObjectManager                              │
│                                                                  │
│  ┌─────────────────────┐    ┌─────────────────────┐             │
│  │     Blueprints      │    │       Clones        │             │
│  │                     │    │                     │             │
│  │  /std/object.c ─────┼────┼─→ (no clones)       │             │
│  │  /std/room.c ───────┼────┼─→ (no clones)       │             │
│  │  /obj/sword.c ──────┼────┼─→ sword#1, sword#2  │             │
│  │  /obj/orc.c ────────┼────┼─→ orc#1, orc#2      │             │
│  └─────────────────────┘    └─────────────────────┘             │
│                                                                  │
│  Operations:                                                     │
│  - load_object(path) → load & compile, return blueprint         │
│  - clone_object(path) → create instance from blueprint          │
│  - find_object(path) → lookup by path                           │
│  - destruct(obj) → remove object from world                     │
└──────────────────────────────────────────────────────────────────┘
```

**Blueprint vs Clone:**
- **Blueprint**: The compiled LPC file itself. Singleton. Created when file is first loaded.
- **Clone**: An instance created from a blueprint. Has its own variable state.

**Object Lifecycle:**
1. `load_object("/obj/sword")` - Parse file, create blueprint, call `create()`
2. `clone_object("/obj/sword")` - Create clone from blueprint, call `create()`
3. Object receives `init()` when another object enters it (or it enters another)
4. `destruct(obj)` - Call `dest()`, remove from world, free resources

### LPC Interpreter

The language runtime, consisting of three stages.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         LPC Interpreter                             │
│                                                                     │
│   Source Code          Tokens              AST            Result    │
│                                                                     │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐     ┌──────────┐ │
│  │          │      │          │      │          │     │          │ │
│  │  int x;  │ ───► │  INT     │ ───► │ VarDecl  │ ──► │  value   │ │
│  │  x = 5;  │      │  IDENT   │      │   └─Assign│    │          │ │
│  │          │      │  SEMI    │      │     └─5  │     │          │ │
│  │          │      │  ...     │      │          │     │          │ │
│  └──────────┘      └──────────┘      └──────────┘     └──────────┘ │
│                                                                     │
│     Lexer              Parser           Interpreter                 │
└─────────────────────────────────────────────────────────────────────┘
```

#### Lexer

Hand-written scanner that converts source text into tokens.

**Token Types:**
- Keywords: `if`, `else`, `while`, `for`, `return`, `inherit`, `int`, `string`, `object`, `mapping`, `mixed`, `void`
- Operators: `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `&&`, `||`, `!`, `=`, `+=`, `-=`
- Literals: integers (`42`), strings (`"hello"`), arrays (`({ 1, 2 })`), mappings (`([ "a": 1 ])`)
- Delimiters: `{`, `}`, `(`, `)`, `[`, `]`, `;`, `,`, `:`
- Identifiers: variable and function names

#### Parser

Recursive descent parser producing an Abstract Syntax Tree.

```
                    ProgramNode
                    /         \
            InheritNode     FunctionNode[]
            "/std/room"           |
                            ┌─────┴─────┐
                            │           │
                      FunctionNode  FunctionNode
                      "create"      "init"
                           │            │
                      BlockStmt     BlockStmt
                           │            │
                      Statement[]   Statement[]
```

**AST Node Types:**
- `ProgramNode` - Top level: inherit declarations + functions
- `FunctionNode` - Return type, name, parameters, body
- `StatementNode` - If, while, for, return, expression, block, variable declaration
- `ExpressionNode` - Binary, unary, call, index, member access, literal, identifier

#### Interpreter

Tree-walking evaluator that executes the AST.

**Runtime Components:**
- `LpcValue` - Tagged union for runtime values (int, string, object, array, mapping)
- `Scope` - Variable bindings with parent chain for lexical scoping
- `CallStack` - Track function calls for `previous_object()` and error traces
- `EfunRegistry` - Map of efun names to C# implementations

### Game Loop

Manages time-based events.

```
┌────────────────────────────────────────────────────────────────┐
│                         GameLoop                                │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    Main Loop                              │  │
│  │                                                          │  │
│  │  while (running) {                                       │  │
│  │      ProcessNetworkIO();      // Handle connections      │  │
│  │      ProcessCallouts();       // Fire due callouts       │  │
│  │      ProcessHeartbeats();     // Call heart_beat()       │  │
│  │      Sleep(tick_interval);    // ~100ms                  │  │
│  │  }                                                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                │
│  ┌─────────────────────┐    ┌─────────────────────┐           │
│  │   Callout Queue     │    │  Heartbeat Registry │           │
│  │                     │    │                     │           │
│  │  time → callback    │    │  object → interval  │           │
│  │  time → callback    │    │  object → interval  │           │
│  │  time → callback    │    │  object → interval  │           │
│  └─────────────────────┘    └─────────────────────┘           │
└────────────────────────────────────────────────────────────────┘
```

**Heartbeats:**
- Objects call `set_heart_beat(1)` to enable
- Driver calls their `heart_beat()` function every N ticks
- Used for: combat rounds, regeneration, NPC AI, environmental effects

**Callouts:**
- `call_out("func", delay, args...)` schedules a delayed call
- `remove_call_out("func")` cancels pending callout
- Used for: spell durations, respawning, delayed effects

## Mudlib Components

### Inheritance Hierarchy

```
                        ┌──────────────┐
                        │   object.c   │
                        │              │
                        │ - id         │
                        │ - short      │
                        │ - long       │
                        └──────┬───────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
              ▼                ▼                ▼
       ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
       │   room.c     │ │  living.c    │ │ container.c  │
       │              │ │              │ │              │
       │ - exits      │ │ - hp/max_hp  │ │ - inventory  │
       │ - items      │ │ - combat     │ │ - capacity   │
       │ - players    │ │ - stats      │ │              │
       └──────────────┘ └──────┬───────┘ └──────────────┘
                               │
                    ┌──────────┴──────────┐
                    │                     │
                    ▼                     ▼
             ┌──────────────┐      ┌──────────────┐
             │  player.c    │      │  monster.c   │
             │              │      │              │
             │ - connection │      │ - ai         │
             │ - commands   │      │ - aggro      │
             │ - save/load  │      │ - loot       │
             └──────────────┘      └──────────────┘
```

### Directory Structure

| Directory | Purpose | Example |
|-----------|---------|---------|
| `/std/` | Base classes inherited by game objects | `object.c`, `room.c`, `living.c` |
| `/obj/` | Clonable items and monsters | `sword.c`, `orc.c`, `potion.c` |
| `/room/` | World areas | `town/square.c`, `dungeon/entrance.c` |
| `/cmds/` | Player commands | `look.c`, `say.c`, `attack.c` |

### Object Communication

```
┌─────────────────────────────────────────────────────────────────┐
│                     Message Flow Example                        │
│                                                                 │
│  Player types: "say Hello everyone!"                            │
│                                                                 │
│  1. Connection receives input                                   │
│  2. Player object parses command                                │
│  3. find command object /cmds/say.c                             │
│  4. say.c executes:                                             │
│                                                                 │
│     tell_object(this_player(), "You say: Hello everyone!\n");   │
│     tell_room(environment(this_player()),                       │
│               "Player says: Hello everyone!\n",                 │
│               ({ this_player() }));  // exclude speaker         │
│                                                                 │
│  5. All players in room receive the message                     │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Player Connection Flow

```
┌──────────┐     connect      ┌──────────┐    create     ┌──────────┐
│  Client  │ ───────────────► │ Network  │ ────────────► │  Player  │
│ (telnet) │                  │  Layer   │               │  Object  │
└──────────┘                  └──────────┘               └──────────┘
     │                                                         │
     │              ┌──────────────────────────────────────────┤
     │              │                                          │
     │              ▼                                          ▼
     │      ┌──────────────┐                           ┌──────────────┐
     │      │   Command    │    "look"                 │    Room      │
     │      │   Parser     │ ◄─────────────────────────│   Object     │
     │      └──────────────┘                           └──────────────┘
     │              │
     │              │ dispatch
     │              ▼
     │      ┌──────────────┐
     │      │  /cmds/look  │
     │      └──────────────┘
     │              │
     │              │ tell_object()
     │◄─────────────┘
     │
   output
```

### Combat Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Combat Sequence                             │
│                                                                     │
│  1. Player: "attack orc"                                            │
│     └─► /cmds/attack.c finds orc in room                            │
│         └─► calls kill(orc) on player                               │
│             └─► player.enemy = orc; orc.enemy = player              │
│                                                                     │
│  2. Game Loop: heartbeat tick                                       │
│     └─► player.heart_beat()                                         │
│         └─► if (enemy) do_attack(enemy)                             │
│             └─► calculate damage, apply to enemy                    │
│                 └─► tell_room("Player hits Orc for 5 damage!")      │
│                                                                     │
│  3. Game Loop: next heartbeat tick                                  │
│     └─► orc.heart_beat()                                            │
│         └─► if (enemy) do_attack(enemy)                             │
│             └─► calculate damage, apply to enemy                    │
│                 └─► tell_room("Orc hits Player for 3 damage!")      │
│                                                                     │
│  4. Repeat until hp <= 0                                            │
│     └─► die() called on loser                                       │
│         └─► create corpse, drop items, destruct monster             │
└─────────────────────────────────────────────────────────────────────┘
```

## CLI Modes

The driver supports multiple execution modes:

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLI Modes                                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Server Mode (default)                                        │   │
│  │ driver --mudlib ./mudlib --port 4000                         │   │
│  │                                                              │   │
│  │ - Load mudlib from specified path                            │   │
│  │ - Start telnet server on port                                │   │
│  │ - Run game loop                                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ REPL Mode                                                    │   │
│  │ driver --repl                                                │   │
│  │                                                              │   │
│  │ - Interactive LPC expression evaluator                       │   │
│  │ - No mudlib required                                         │   │
│  │ - Useful for testing language features                       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Eval Mode                                                    │   │
│  │ driver --eval "5 + 3 * 2"                                    │   │
│  │                                                              │   │
│  │ - Evaluate single expression                                 │   │
│  │ - Print result and exit                                      │   │
│  │ - Useful for scripts                                         │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Run Mode                                                     │   │
│  │ driver --mudlib ./mudlib --run test.c --call test_func       │   │
│  │                                                              │   │
│  │ - Load mudlib for full context                               │   │
│  │ - Load specified file                                        │   │
│  │ - Call specified function                                    │   │
│  │ - Print result and exit                                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Test Mode                                                    │   │
│  │ driver --mudlib ./mudlib --test ./lpc-tests/                 │   │
│  │                                                              │   │
│  │ - Load mudlib for full context                               │   │
│  │ - Find all .c files in test directory                        │   │
│  │ - Call run_tests() on each                                   │   │
│  │ - Report pass/fail summary                                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## Security Model

The driver provides a natural sandbox through the efun interface:

```
┌─────────────────────────────────────────────────────────────────────┐
│                       Security Boundary                             │
│                                                                     │
│  LPC Code CAN:                    LPC Code CANNOT:                  │
│  ─────────────                    ────────────────                  │
│  - Call registered efuns          - Access filesystem directly      │
│  - Create/modify LPC objects      - Open network connections        │
│  - Send messages to players       - Execute system commands         │
│  - Read/write object variables    - Access C# internals             │
│  - Use arrays and mappings        - Import arbitrary libraries      │
│                                                                     │
│  The driver controls:                                               │
│  - Which efuns are available                                        │
│  - File paths LPC can access (sandboxed to mudlib)                  │
│  - Resource limits (memory, recursion depth)                        │
└─────────────────────────────────────────────────────────────────────┘
```

## Future Considerations

### Domains System
Organize mudlib into areas owned by different builders:
```
/domains/
  /town/
    /room/
    /obj/
  /forest/
    /room/
    /obj/
```

### Multiple Inheritance
Current design uses single inheritance. The architecture supports adding multiple inheritance later by:
- Modifying the parser to accept multiple `inherit` statements
- Implementing method resolution order (MRO) in the interpreter
- Handling diamond problem scenarios

### WebSocket Support
Add browser-based client support alongside telnet:
- WebSocket listener in network layer
- JSON message protocol
- Same player objects, different transport
