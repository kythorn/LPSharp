# Implementation Plan

This document outlines the phased implementation approach for the LPMud Revival project. Development proceeds incrementally, with each milestone producing something runnable and testable.

## Working Style

After each milestone:
1. Review the code together
2. Run the demo
3. Ask questions, suggest changes
4. Proceed to next milestone only after approval

This ensures you can follow along and understand the progress as it happens.

---

## Milestone 1: Lexer

**Goal**: Tokenize LPC source code into a stream of tokens.

**What We're Building**:
- `Token.cs` - Token type enum and token record
- `Lexer.cs` - Hand-written scanner

**Demo**: Feed LPC text, see token stream printed.

**Example**:
```
Input:  int x = 5 + 3;
Output: [INT, IDENTIFIER("x"), ASSIGN, NUMBER(5), PLUS, NUMBER(3), SEMICOLON]
```

**Token Types to Support**:
- Keywords: `if`, `else`, `while`, `for`, `return`, `inherit`, `int`, `string`, `object`, `mapping`, `mixed`, `void`
- Operators: `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `&&`, `||`, `!`, `=`
- Literals: integers, strings
- Delimiters: `{`, `}`, `(`, `)`, `[`, `]`, `;`, `,`
- Identifiers: variable and function names
- Comments: `//` single-line, `/* */` multi-line

**Deliverables**:
- [ ] Token.cs with all token types
- [ ] Lexer.cs with NextToken() method
- [ ] LexerTests.cs with unit tests
- [ ] CLI can tokenize a file and print tokens

---

## Milestone 2: Parser

**Goal**: Parse token stream into an Abstract Syntax Tree (AST).

**What We're Building**:
- `AstNodes.cs` - All AST node types
- `Parser.cs` - Recursive descent parser

**Demo**: Parse a simple function, print AST structure.

**Example**:
```c
int add(int a, int b) {
    return a + b;
}
```
Produces:
```
FunctionNode
├── ReturnType: int
├── Name: add
├── Parameters: [(int, a), (int, b)]
└── Body: BlockStatement
    └── ReturnStatement
        └── BinaryExpression
            ├── Left: Identifier(a)
            ├── Operator: +
            └── Right: Identifier(b)
```

**AST Nodes to Support**:
- `ProgramNode` - List of inherits + functions
- `InheritNode` - Inherit declaration
- `FunctionNode` - Function definition
- `ParameterNode` - Function parameter
- `BlockStatement` - `{ ... }`
- `IfStatement` - `if (cond) { } else { }`
- `WhileStatement` - `while (cond) { }`
- `ForStatement` - `for (init; cond; incr) { }`
- `ReturnStatement` - `return expr;`
- `ExpressionStatement` - `expr;`
- `VariableDeclaration` - `int x = 5;`
- `BinaryExpression` - `a + b`
- `UnaryExpression` - `!x`, `-x`
- `CallExpression` - `func(args)`
- `IndexExpression` - `arr[0]`
- `Identifier` - Variable reference
- `Literal` - Number, string, array, mapping

**Deliverables**:
- [ ] AstNodes.cs with all node types
- [ ] Parser.cs with recursive descent implementation
- [ ] ParserTests.cs with unit tests
- [ ] CLI can parse a file and print AST

---

## Milestone 3: Interpreter + REPL

**Goal**: Evaluate expressions and simple statements, with interactive REPL.

**What We're Building**:
- `LpcValue.cs` - Runtime value representation
- `Scope.cs` - Variable binding environment
- `Interpreter.cs` - Tree-walking evaluator
- `Repl.cs` - Interactive read-eval-print loop

**Demo**:
```bash
$ driver --repl
LPC> 5 + 3 * 2
11
LPC> string x = "hello";
LPC> x + " world"
"hello world"
LPC> ({ 1, 2 }) + ({ 3 })
({ 1, 2, 3 })
```

**Features**:
- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
- Logical: `&&`, `||`, `!`
- String concatenation: `"a" + "b"`
- Array literals: `({ 1, 2, 3 })`
- Mapping literals: `([ "key": value ])`
- Variable declaration and assignment
- Operator precedence

**Deliverables**:
- [ ] LpcValue.cs with tagged union for runtime types
- [ ] Scope.cs for variable bindings
- [ ] Interpreter.cs for expression evaluation
- [ ] Repl.cs for interactive mode
- [ ] InterpreterTests.cs with unit tests
- [ ] `--repl` and `--eval` CLI modes working

---

## Milestone 4: Functions & Variables

**Goal**: Define and call functions with proper scoping.

**What We're Building**:
- Extend `Interpreter.cs` for function definitions and calls
- Local variable scoping

**Demo**:
```bash
$ driver --run test.c --call test_it

# test.c:
int double(int x) {
    return x * 2;
}

int test_it() {
    return double(21);  // Returns 42
}
```

**Features**:
- Function definitions with return types
- Function calls with arguments
- Local variables with block scoping
- Return statements

**Deliverables**:
- [ ] Function definition parsing and storage
- [ ] Function call execution
- [ ] Local variable scoping
- [ ] Return value handling
- [ ] `--run` CLI mode working
- [ ] Additional unit tests

---

## Milestone 5: Object Model

**Goal**: Objects with inheritance and lifecycle hooks.

**What We're Building**:
- `MudObject.cs` - Base class for LPC objects
- `ObjectManager.cs` - Load, clone, find, destroy

**Demo**:
```c
// /std/object.c
void create() {
    write("Base object created\n");
}

// /obj/thing.c
inherit "/std/object";

void create() {
    ::create();  // Call parent
    write("Thing created\n");
}
```

**Features**:
- Load LPC file as object
- `inherit` statement processing
- Parent function access via `::`
- `create()` called on load/clone
- `clone_object()` efun
- Object variables (instance state)
- `this_object()` efun

**Deliverables**:
- [x] MudObject.cs with variable storage
- [x] ObjectManager.cs for lifecycle management
- [x] Inheritance resolution
- [x] `create()` hook
- [x] `clone_object()`, `this_object()`, `load_object()`, `find_object()`, `destruct()` efuns
- [x] Unit tests for object model (16 tests passing)
- [x] Local scope for function parameters
- [x] Object-centric execution model
- [x] Thread-safe blueprint caching

---

## Milestone 6: Telnet Server

**Goal**: Single-threaded game loop architecture with proper command/output queuing.

**What We're Building**:
- `GameLoop.cs` - Single-threaded game loop for all LPC execution
- `ExecutionContext.cs` - Thread-local context for this_player() and output routing
- `TelnetServer.cs` - TCP listener with output draining
- `Connection.cs` - Per-client state with command queuing

**Demo**:
```bash
$ dotnet run --project src/Driver/Driver -- --server --mudlib ./mudlib --port 4000
$ telnet localhost 4000
Welcome to LPMud Revival!

> say Hello, world!
You say: Hello, world!
> look
You are in a void.
There is nothing here yet.
> quit
Goodbye!
Connection closed.
```

**Architecture**:
```
CURRENT: Connection → Interpreter (isolated)
TARGET:  Connections → Command Queue → GameLoop (single thread) → Output Queue → Connections
```

**Features**:
- Listen on TCP port
- Accept multiple connections
- Single-threaded game loop (no race conditions)
- Command queue for player input
- Output queue for message routing
- ExecutionContext pattern for this_player() and write()
- Graceful disconnect handling

**Deliverables**:
- [x] TelnetServer.cs with output queue draining
- [x] Connection.cs with command queuing
- [x] GameLoop.cs with single-threaded command processing
- [x] ExecutionContext.cs for thread-local context
- [x] Unit tests for GameLoop and ExecutionContext
- [x] Integration tests for multi-connection scenarios

---

## Milestone 7: Player & Commands

**Goal**: Connect player objects to connections and parse commands.

**What We're Building**:
- `/std/player.c` - Player base class
- Command routing system
- Basic commands: `say`, `look`, `quit`

**Demo**:
```
$ telnet localhost 4000
Welcome to LPMud Revival!

> say Hello everyone!
You say: Hello everyone!
> look
You are in a void.
There is nothing here yet.
> quit
Goodbye!
Connection closed.
```

**Features**:
- Create player object on connect (clone of /std/player.c)
- Route input to command parser
- Find command in `/cmds/` directory
- Execute command's main() with arguments
- `tell_object()` for output to player
- `this_player()` efun
- Player object cleanup on disconnect

**Deliverables**:
- [x] /std/player.c base class
- [x] Command lookup and dispatch in GameLoop
- [x] /cmds/say.c command
- [x] /cmds/look.c command (basic version)
- [x] /cmds/quit.c command
- [x] tell_object() and this_player() efuns
- [x] Integration tests

---

## Milestone 8: Rooms & Movement

**Goal**: Rooms with descriptions and exits, movement commands.

**What We're Building**:
- `/std/room.c` - Room base class
- `/room/start.c` - Starting room
- `look` and `go` commands

**Demo**:
```
> look
Town Square
You are standing in the town square. A fountain bubbles nearby.
Obvious exits: north, east, south

> go north
You go north.

> look
Market Street
Merchants hawk their wares along this busy street.
Obvious exits: south, west
```

**Features**:
- Room with short/long descriptions
- Exit definitions (direction → destination)
- `look` command shows room
- `go <direction>` moves player
- `tell_room()` for messages to room
- `environment()` efun
- `move_object()` efun
- `init()` called when entering room

**Deliverables**:
- [ ] /std/room.c with exits and descriptions
- [ ] /room/start.c starting location
- [ ] /room/market.c connected room
- [ ] /cmds/look.c command
- [ ] /cmds/go.c command
- [ ] Movement and room messaging efuns
- [ ] Integration tests for movement

---

## Milestone 9: Heartbeats & Callouts

**Goal**: Time-based callbacks working.

**What We're Building**:
- `GameLoop.cs` - Heartbeat and callout scheduling
- Heartbeat registration

**Demo**:
```c
// A room that has ambient messages
void create() {
    ::create();
    set_heart_beat(1);
}

void heart_beat() {
    if (random(10) == 0) {
        tell_room(this_object(), "A bird chirps in the distance.\n");
    }
}
```

**Features**:
- `set_heart_beat(on/off)` efun
- Driver calls `heart_beat()` periodically
- `call_out(func, delay, args)` efun
- `remove_call_out(func)` efun
- Configurable tick rate

**Deliverables**:
- [ ] GameLoop.cs with timing system
- [ ] Heartbeat registration and dispatch
- [ ] Callout scheduling and dispatch
- [ ] set_heart_beat(), call_out(), remove_call_out() efuns
- [ ] Unit tests for timing

---

## Milestone 10: Combat

**Goal**: Full combat loop with attacking, damage, and death.

**What We're Building**:
- `/std/living.c` - Combat-capable base class
- `/std/monster.c` - NPC base class
- `/obj/orc.c` - Example monster
- `attack` command
- Death handling

**Demo**:
```
> look
Dark Cave
A menacing orc guards this cave.
Obvious exits: out

> attack orc
You attack the Orc!

[combat round]
You hit the Orc for 8 damage!
The Orc hits you for 5 damage!

[combat round]
You hit the Orc for 12 damage!
The Orc misses you!

[combat round]
You hit the Orc for 10 damage!
The Orc dies!
You gain 50 experience points.
```

**Features**:
- HP, attack, defense stats
- `attack` command initiates combat
- Heartbeat-driven combat rounds
- Damage calculation
- Death handling
- Monster respawning (via callout)

**Deliverables**:
- [ ] /std/living.c with HP, combat functions
- [ ] /std/monster.c with AI basics
- [ ] /obj/orc.c example monster
- [ ] /cmds/attack.c command
- [ ] Combat round logic
- [ ] Death and cleanup
- [ ] Integration test for full combat

---

## Post-Milestone 10: Polish & Extensions

After the core milestones, potential additions:

- **Inventory**: `get`, `drop`, `inventory` commands
- **Equipment**: Weapons and armor that modify stats
- **More monsters**: Variety with different behaviors
- **Areas**: Expand the world
- **Persistence**: Save/load player stats
- **Domains**: Organize mudlib by builder areas

---

## Testing Strategy

### Unit Tests (C# / xUnit)

Run with: `dotnet test`

| Test File | Coverage |
|-----------|----------|
| LexerTests.cs | Token sequences for various inputs |
| ParserTests.cs | AST structure verification |
| InterpreterTests.cs | Expression evaluation, scoping |
| ObjectManagerTests.cs | Load, clone, inherit |

### Integration Tests

- Spin up driver with `--mudlib ./test-mudlib`
- Programmatically connect
- Send commands, assert output
- Test full flows

### LPC Tests

```bash
$ driver --mudlib ./mudlib --test ./lpc-tests/
```

Test files use `assert()` efun:
```c
// lpc-tests/test_arrays.c
void run_tests() {
    assert(({ 1, 2 }) + ({ 3 }) == ({ 1, 2, 3 }), "array concat");
    assert(sizeof(({ "a", "b" })) == 2, "sizeof");
}
```

---

## Current Status

| Milestone | Status |
|-----------|--------|
| 1. Lexer | ✅ Complete |
| 2. Parser | ✅ Complete |
| 3. Interpreter + REPL | ✅ Complete |
| 4. Functions & Variables | ✅ Complete |
| 5. Object Model | ✅ Complete |
| 6. Telnet Server | ✅ Complete |
| 7. Player & Commands | ✅ Complete |
| 8. Rooms & Movement | Not Started |
| 9. Heartbeats & Callouts | Not Started |
| 10. Combat | Not Started |

---

## Current State Notes

### What's Working (as of Milestone 7 complete)

**Interpreter/REPL:**
- Full expression evaluation with correct operator precedence
- Variables: assignment (`x = 5`), compound (`x += 3`), increment/decrement (`++x`, `x--`)
- All operators: arithmetic, comparison, logical, bitwise, ternary
- String concatenation and comparison
- Control flow: if/else, while, for, break, continue, return
- User-defined functions with parameters and return values
- Efuns: `write()`, `typeof()`, `strlen()`, `to_string()`, `to_int()`
- 374+ unit tests passing

**Object Model (Milestone 5):**
- Blueprint/clone architecture (singletons vs instances)
- Inheritance with `inherit` statement
- Parent function calls with `::` operator
- Object-centric execution (all code runs within object context)
- Lifecycle hooks (`create()` called on load/clone)
- Object variables (persistent state per instance)
- Local scope for function parameters
- Object efuns: `clone_object()`, `this_object()`, `load_object()`, `find_object()`, `destruct()`
- Thread-safe ObjectManager with concurrent blueprint caching

**Single-Threaded Game Loop (Milestones 6+7):**
- `GameLoop.cs` - Single-threaded command processing (avoids all race conditions)
- `ExecutionContext.cs` - Thread-local context for `this_player()` and output routing
- Command queue for player input from network threads
- Output queue for message routing back to connections
- Player objects created on connect (clone of /std/player.c)
- Command routing: parse verb, load /cmds/{verb}.c, call main(args)
- Player object cleanup on disconnect

**Networking:**
- Telnet server accepts multiple concurrent connections (`--server [port]`)
- Each connection gets its own player object
- `write()` routes output to correct connection via ExecutionContext
- `this_player()` returns the player executing the current command
- `tell_object()` sends messages to specific players
- Line buffering with basic telnet protocol handling
- Graceful quit/exit and Ctrl+C shutdown

**Commands:**
- `/cmds/say.c` - Say a message
- `/cmds/look.c` - Look at surroundings (basic version)
- `/cmds/quit.c` - Quit and disconnect

### What's NOT Working Yet

**Language gaps:**
- No arrays or mappings (syntax exists but not implemented)
- No call_other syntax (`obj->func()`)
- No mapping/array indexing and manipulation

**Gameplay gaps (needed for Milestone 8+):**
- No rooms - players exist in a void
- No `environment()` or `move_object()` efuns
- No `tell_room()` for room-wide messaging
- No exits or movement

**Execution Limits (Safety):**
- Instruction counter limits execution to 1,000,000 instructions per command (configurable)
- Recursion depth limited to 100 levels (configurable)
- Limits can be disabled for testing or admin commands
- Commands that exceed limits are aborted with a clear error message
- Player connection remains active after limit violation
