# Driver Fixes Before Mudlib Development

This document tracks critical driver fixes needed before serious mudlib development.

## Priority 1: call_other Security

**Status:** COMPLETE

**Problem:** `call_other()` can call any function on any object, including private functions. This breaks encapsulation and enables exploits.

**Solution (implemented):**
- Added function visibility modifiers: public (default), private, static, protected, nomask
- `call_other()` and arrow operator (`->`) only call public functions
- Private functions are not inherited (local to defining object only)
- Static functions ARE inherited (LPC semantics) but not call_other callable
- Protected functions inherited but not call_other callable
- Nomask functions cannot be overridden

**Files modified:**
- `Ast.cs` - FunctionVisibility enum
- `Token.cs` - visibility modifier tokens
- `Lexer.cs` - visibility keywords
- `Parser.cs` - parse visibility modifiers
- `ObjectInterpreter.cs` - visibility checks in call_other and arrow
- `LpcProgram.cs` - exclude private functions from inheritance
- `docs/LPC-REFERENCE.md` - documentation with examples

---

## Priority 2: Duplicate Login Handling

**Status:** COMPLETE

**Problem:** Same account can log in multiple times simultaneously, creating multiple player objects.

**Solution (implemented):**
- On login, check if username already has active (Playing) session
- Prompt new user: "You are already logged in... Do you want to take over? (y/n)"
- Y: kick existing session (notified: "Another login detected"), complete new login
- N: disconnect new session ("Login cancelled")
- Race condition protection via minimal lock only for session state transition
- Final safety check in CompleteLogin handles rare concurrent logins

**Files modified:**
- `LoginState.cs` - Added `ConfirmTakeover` state
- `GameLoop.cs` - Added `FindSessionByUsername()`, `KickSession()`, `CheckAndPromptForDuplicateSession()`, `HandleConfirmTakeover()`

---

## Priority 3: Linkdead System

**Status:** TODO

**Problem:** When connection drops, player object is immediately destructed. Player loses all progress.

**Solution:**
- On disconnect, mark player as "linkdead" instead of destructing
- Player object persists for configurable time (default: 15 minutes)
- Other players see "(linkdead)" status
- If player reconnects within timeout, take over existing session
- After timeout, save player data and destruct
- Remove from combat on linkdead

**Files to modify:**
- `GameLoop.cs` - RemovePlayerSession(), session management
- `ExecutionContext.cs` - PlayerSession state
- `AccountManager.cs` - player data persistence

---

## Priority 4: Graceful Shutdown

**Status:** TODO

**Problem:** Server shutdown (Ctrl+C) doesn't save player data. All progress lost.

**Solution:**
- Intercept shutdown signal
- Announce shutdown to all players
- Save all player data (call save_object or equivalent)
- Disconnect all players cleanly
- Flush any pending writes

**Files to modify:**
- `Program.cs` - signal handling
- `TelnetServer.cs` - shutdown sequence
- `GameLoop.cs` - save all players

---

## Lower Priority (Can defer)

### Better Error Reporting
- Add line numbers to LPC errors
- Show stack traces in debug mode
- Structured logging with levels

### Inherit Security
- Validate inherit paths at compile time
- Prevent inheriting from /secure/ without permission

### Configuration File
- JSON/INI config file for server settings
- Port, mudlib path, limits, starting room, etc.

### ANSI Color Support
- Color efuns for LPC code
- Client capability detection

---

## Completion Checklist

- [x] call_other security (function visibility)
- [x] Duplicate login handling
- [ ] Linkdead system with reconnection
- [ ] Graceful shutdown with player saves
- [ ] (Optional) Better error reporting
- [ ] (Optional) Inherit security at compile time
- [ ] (Optional) Configuration file
- [ ] (Optional) ANSI colors
