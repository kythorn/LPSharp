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

**Status:** COMPLETE

**Problem:** When connection drops, player object is immediately destructed. Player loses all progress.

**Solution (implemented):**
- On disconnect, Playing sessions move to linkdead instead of destructing
- Player object persists for 15 minutes (configurable via `LinkdeadTimeout`)
- Room receives announcement: "Name has gone linkdead." (using character name, not account)
- If player reconnects within timeout, auto-reconnect to existing session (no prompt)
- After timeout, announce "Name has disconnected (linkdead timeout)." and destruct
- New efuns: `linkdead_users()`, `query_linkdead(object)`
- Added `who` command that shows active and linkdead players

**Files modified:**
- `ExecutionContext.cs` - Added `IsLinkdead`, `LinkdeadSince` to PlayerSession; made `ConnectionId` mutable
- `GameLoop.cs` - Added `_linkdeadSessions` dictionary, `RemovePlayerSession()` moves to linkdead, `ReconnectToLinkdeadSession()`, `CleanupExpiredLinkdeadSessions()`, `GetPlayerName()` helper for character names
- `ObjectInterpreter.cs` - Added `linkdead_users()` and `query_linkdead()` efuns
- `mudlib/cmds/std/who.c` - New command showing players and linkdead status

---

## Priority 4: Graceful Shutdown

**Status:** COMPLETE

**Problem:** Server shutdown (Ctrl+C) doesn't save player data. All progress lost.

**Solution (implemented):**
- Ctrl+C handler in TelnetServer calls GracefulShutdown()
- Admin `shutdown` command available via `shutdown()` efun
- GracefulShutdown announces to all players: "Server shutting down. Saving your character..."
- Saves all active and linkdead players before stopping
- `save_object()` / `restore_object()` efuns for LPC object persistence
- Player data saved on: quit, linkdead timeout, shutdown, and every 5 minutes
- Player data restored on login

**Files modified:**
- `TelnetServer.cs` - Stop() calls GracefulShutdown()
- `GameLoop.cs` - GracefulShutdown(), SaveAllPlayers(), SavePlayerObject(), periodic saves
- `ObjectInterpreter.cs` - save_object, restore_object, shutdown efuns
- `mudlib/std/player.c` - save_player(), restore_player() functions
- `mudlib/cmds/std/quit.c` - calls save_player before destruct
- `mudlib/cmds/admin/shutdown.c` - admin shutdown command
- `mudlib/secure/players/` - directory for player save files

---

## Lower Priority (Can defer)

### Better Error Reporting
- [x] Add line numbers to LPC errors (implemented - LpcRuntimeException)
- [x] Show stack traces (implemented - nested function calls show trace)
- [ ] Structured logging with levels

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
- [x] Linkdead system with reconnection
- [x] Graceful shutdown with player saves
- [ ] (Optional) Better error reporting
- [ ] (Optional) Inherit security at compile time
- [ ] (Optional) Configuration file
- [ ] (Optional) ANSI colors
