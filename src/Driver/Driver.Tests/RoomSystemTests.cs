using Xunit;

namespace Driver.Tests;

/// <summary>
/// Tests for the room and movement system.
/// Tests the C# infrastructure (MudObject environment/contents).
/// LPC room functionality is tested via manual testing or integration tests.
/// </summary>
public class RoomSystemTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;

    public RoomSystemTests()
    {
        // Create a temporary mudlib directory with minimal objects
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_room_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));

        // Create minimal base object
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { short_desc = ""something""; }
string query_short() { return short_desc; }
void set_short(string s) { short_desc = s; }
");

        // Create minimal player
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "player.c"), @"
inherit ""/std/object"";
void create() { ::create(); set_short(""a player""); }
");

        // Create minimal room (no complex LPC logic)
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "room.c"), @"
inherit ""/std/object"";
void create() { ::create(); set_short(""a room""); }
");

        _objectManager = new ObjectManager(_testMudlibPath);
        _objectManager.InitializeInterpreter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testMudlibPath))
        {
            try { Directory.Delete(_testMudlibPath, true); }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public void MudObject_HasEnvironmentProperty()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        Assert.Null(player.Environment);

        player.MoveTo(room);
        Assert.Equal(room, player.Environment);
    }

    [Fact]
    public void MudObject_HasContentsProperty()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        Assert.Empty(room.Contents);

        player.MoveTo(room);
        Assert.Contains(player, room.Contents);
    }

    [Fact]
    public void MudObject_MoveTo_UpdatesEnvironment()
    {
        var room1 = _objectManager.LoadObject("/std/room");
        var room2 = _objectManager.CloneObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        player.MoveTo(room1);
        Assert.Equal(room1, player.Environment);

        player.MoveTo(room2);
        Assert.Equal(room2, player.Environment);
    }

    [Fact]
    public void MudObject_MoveTo_RemovesFromOldEnvironment()
    {
        var room1 = _objectManager.LoadObject("/std/room");
        var room2 = _objectManager.CloneObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        player.MoveTo(room1);
        Assert.Contains(player, room1.Contents);

        player.MoveTo(room2);
        Assert.DoesNotContain(player, room1.Contents);
        Assert.Contains(player, room2.Contents);
    }

    [Fact]
    public void MudObject_MoveTo_Null_RemovesFromEnvironment()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        player.MoveTo(room);
        Assert.Equal(room, player.Environment);

        player.MoveTo(null);
        Assert.Null(player.Environment);
        Assert.DoesNotContain(player, room.Contents);
    }

    [Fact]
    public void MudObject_MoveTo_Self_ReturnsFalse()
    {
        var player = _objectManager.CloneObject("/std/player");

        var result = player.MoveTo(player);
        Assert.False(result);
    }

    [Fact]
    public void MudObject_Contains_ReturnsTrue_ForDirectContents()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        player.MoveTo(room);
        Assert.True(room.Contains(player));
    }

    [Fact]
    public void MudObject_Contains_ReturnsFalse_WhenNotContained()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player = _objectManager.CloneObject("/std/player");

        Assert.False(room.Contains(player));
    }

    [Fact]
    public void MultipleObjects_CanBeInSameRoom()
    {
        var room = _objectManager.LoadObject("/std/room");
        var player1 = _objectManager.CloneObject("/std/player");
        var player2 = _objectManager.CloneObject("/std/player");

        player1.MoveTo(room);
        player2.MoveTo(room);

        Assert.Equal(2, room.Contents.Count);
        Assert.Contains(player1, room.Contents);
        Assert.Contains(player2, room.Contents);
    }
}

/// <summary>
/// Tests for init() hook being called when objects enter environments.
/// </summary>
public class InitHookTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public InitHookTests()
    {
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_init_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "room"));

        // Create minimal base object
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { short_desc = ""something""; }
string query_short() { return short_desc; }
void set_short(string s) { short_desc = s; }
");

        // Create player that tracks init calls
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "player.c"), @"
inherit ""/std/object"";
int init_called;
void create() { ::create(); init_called = 0; }
void init() { init_called = init_called + 1; }
int query_init_called() { return init_called; }
");

        // Create room that tracks init calls
        File.WriteAllText(Path.Combine(_testMudlibPath, "room", "test_room.c"), @"
inherit ""/std/object"";
int init_called;
object last_this_player;
void create() { ::create(); init_called = 0; last_this_player = 0; }
void init() {
    init_called = init_called + 1;
    last_this_player = this_player();
}
int query_init_called() { return init_called; }
object query_last_this_player() { return last_this_player; }
");

        // Create NPC that tracks init calls
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "npc.c"), @"
inherit ""/std/object"";
int init_called;
void create() { ::create(); init_called = 0; }
void init() { init_called = init_called + 1; }
int query_init_called() { return init_called; }
");

        _objectManager = new ObjectManager(_testMudlibPath);
        _objectManager.InitializeInterpreter();
        _interpreter = new ObjectInterpreter(_objectManager);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testMudlibPath))
        {
            try { Directory.Delete(_testMudlibPath, true); }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public void Init_CalledOnDestination_WhenObjectEnters()
    {
        var room = _objectManager.LoadObject("/room/test_room");
        var player = _objectManager.CloneObject("/std/player");

        // Verify init not called yet
        var initCalled = _interpreter.CallFunctionOnObject(room, "query_init_called", new List<object>());
        Assert.Equal(0, initCalled);

        // Move player to room using move_object efun
        _interpreter.CallFunctionOnObject(player, "create", new List<object>()); // Ensure created

        // Use the efun to move (which should trigger init)
        var moveResult = _interpreter.CallEfun("move_object", new List<object> { player, room });
        Assert.Equal(1, moveResult);

        // Verify init was called on the room
        initCalled = _interpreter.CallFunctionOnObject(room, "query_init_called", new List<object>());
        Assert.Equal(1, initCalled);
    }

    [Fact]
    public void Init_ThisPlayer_IsMovingObject()
    {
        var room = _objectManager.LoadObject("/room/test_room");
        var player = _objectManager.CloneObject("/std/player");

        // Move player to room
        _interpreter.CallEfun("move_object", new List<object> { player, room });

        // Verify this_player() during init was the player
        var lastThisPlayer = _interpreter.CallFunctionOnObject(room, "query_last_this_player", new List<object>());
        Assert.Equal(player, lastThisPlayer);
    }

    [Fact]
    public void Init_CalledOnOtherObjects_WhenObjectEnters()
    {
        var room = _objectManager.LoadObject("/room/test_room");
        var npc = _objectManager.CloneObject("/std/npc");
        var player = _objectManager.CloneObject("/std/player");

        // Put NPC in room first (using direct MoveTo to avoid init complications)
        npc.MoveTo(room);

        // Verify NPC init not called yet
        var npcInitCalled = _interpreter.CallFunctionOnObject(npc, "query_init_called", new List<object>());
        Assert.Equal(0, npcInitCalled);

        // Move player to room via efun
        _interpreter.CallEfun("move_object", new List<object> { player, room });

        // Verify init was called on the NPC (because player entered)
        npcInitCalled = _interpreter.CallFunctionOnObject(npc, "query_init_called", new List<object>());
        Assert.Equal(1, npcInitCalled);
    }

    [Fact]
    public void Init_NotCalled_WhenMovingToNull()
    {
        var room = _objectManager.LoadObject("/room/test_room");
        var player = _objectManager.CloneObject("/std/player");

        // Put player in room first
        player.MoveTo(room);

        // Reset init counter
        room.SetVariable("init_called", 0);

        // Move player to null (remove from environment)
        _interpreter.CallEfun("move_object", new List<object> { player, 0 });

        // Verify init was NOT called on room
        var initCalled = _interpreter.CallFunctionOnObject(room, "query_init_called", new List<object>());
        Assert.Equal(0, initCalled);
    }
}
