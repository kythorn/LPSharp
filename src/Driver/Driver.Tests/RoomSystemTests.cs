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
        Assert.Equal(0L, initCalled);

        // Move player to room using move_object efun
        _interpreter.CallFunctionOnObject(player, "create", new List<object>()); // Ensure created

        // Use the efun to move (which should trigger init)
        var moveResult = _interpreter.CallEfun("move_object", new List<object> { player, room });
        Assert.Equal(1L, moveResult);

        // Verify init was called on the room
        initCalled = _interpreter.CallFunctionOnObject(room, "query_init_called", new List<object>());
        Assert.Equal(1L, initCalled);
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
        Assert.Equal(0L, npcInitCalled);

        // Move player to room via efun
        _interpreter.CallEfun("move_object", new List<object> { player, room });

        // Verify init was called on the NPC (because player entered)
        npcInitCalled = _interpreter.CallFunctionOnObject(npc, "query_init_called", new List<object>());
        Assert.Equal(1L, npcInitCalled);
    }

    [Fact]
    public void Init_NotCalled_WhenMovingToNull()
    {
        var room = _objectManager.LoadObject("/room/test_room");
        var player = _objectManager.CloneObject("/std/player");

        // Put player in room first
        player.MoveTo(room);

        // Reset init counter
        room.SetVariable("init_called", 0L);

        // Move player to null (remove from environment)
        _interpreter.CallEfun("move_object", new List<object> { player, 0 });

        // Verify init was NOT called on room
        var initCalled = _interpreter.CallFunctionOnObject(room, "query_init_called", new List<object>());
        Assert.Equal(0L, initCalled);
    }
}

/// <summary>
/// Tests for present() efun - finding objects by name.
/// </summary>
public class PresentEfunTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public PresentEfunTests()
    {
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_present_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "obj"));

        // Create minimal base object with id() function
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
string obj_id;
void create() { short_desc = ""something""; obj_id = """"; }
string query_short() { return short_desc; }
void set_short(string s) { short_desc = s; }
void set_id(string id) { obj_id = id; }
int id(string name) {
    if (obj_id != """" && name == obj_id) return 1;
    return 0;
}
");

        // Create a simple sword item
        File.WriteAllText(Path.Combine(_testMudlibPath, "obj", "sword.c"), @"
inherit ""/std/object"";
void create() {
    ::create();
    set_short(""a rusty sword"");
    set_id(""sword"");
}
");

        // Create a simple shield item
        File.WriteAllText(Path.Combine(_testMudlibPath, "obj", "shield.c"), @"
inherit ""/std/object"";
void create() {
    ::create();
    set_short(""a wooden shield"");
    set_id(""shield"");
}
");

        // Create a room
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "room.c"), @"
inherit ""/std/object"";
void create() {
    ::create();
    set_short(""a room"");
}
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
    public void Present_FindsObjectById()
    {
        var room = _objectManager.LoadObject("/std/room");
        var sword = _objectManager.CloneObject("/obj/sword");

        sword.MoveTo(room);

        var result = _interpreter.CallEfun("present", new List<object> { "sword", room });
        Assert.Equal(sword, result);
    }

    [Fact]
    public void Present_ReturnsZeroWhenNotFound()
    {
        var room = _objectManager.LoadObject("/std/room");
        var sword = _objectManager.CloneObject("/obj/sword");

        sword.MoveTo(room);

        var result = _interpreter.CallEfun("present", new List<object> { "axe", room });
        Assert.Equal(0L, result);
    }

    [Fact]
    public void Present_FindsSecondObjectWithIndex()
    {
        var room = _objectManager.LoadObject("/std/room");
        var sword1 = _objectManager.CloneObject("/obj/sword");
        var sword2 = _objectManager.CloneObject("/obj/sword");

        sword1.MoveTo(room);
        sword2.MoveTo(room);

        // "sword" finds first
        var result1 = _interpreter.CallEfun("present", new List<object> { "sword", room });
        Assert.Equal(sword1, result1);

        // "sword 2" finds second
        var result2 = _interpreter.CallEfun("present", new List<object> { "sword 2", room });
        Assert.Equal(sword2, result2);
    }

    [Fact]
    public void Present_RequiresIdFunction()
    {
        // Objects without a matching id() function should NOT be found
        var room = _objectManager.LoadObject("/std/room");
        var container = _objectManager.CloneObject("/std/room");
        room.MoveTo(container);

        // "room" is in the short description but id() doesn't match it
        // (base object.c id() only matches obj_id which is empty)
        var result = _interpreter.CallEfun("present", new List<object> { "room", container });
        Assert.Equal(0L, result); // NOT found - no fallback to short desc
    }

    [Fact]
    public void Present_IsCaseInsensitive()
    {
        var room = _objectManager.LoadObject("/std/room");
        var sword = _objectManager.CloneObject("/obj/sword");

        sword.MoveTo(room);

        var result = _interpreter.CallEfun("present", new List<object> { "SWORD", room });
        Assert.Equal(sword, result);
    }

    [Fact]
    public void Present_ChecksIfObjectIsInContainer()
    {
        var room = _objectManager.LoadObject("/std/room");
        var sword = _objectManager.CloneObject("/obj/sword");
        var shield = _objectManager.CloneObject("/obj/shield");

        sword.MoveTo(room);
        // shield is NOT in room

        // present(object, container) checks if object is in container
        var resultSword = _interpreter.CallEfun("present", new List<object> { sword, room });
        Assert.Equal(sword, resultSword);

        var resultShield = _interpreter.CallEfun("present", new List<object> { shield, room });
        Assert.Equal(0L, resultShield);
    }
}

/// <summary>
/// Tests for arrow syntax: obj->func(args)
/// </summary>
public class ArrowSyntaxTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public ArrowSyntaxTests()
    {
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_arrow_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "obj"));

        // Create base object with query functions
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
int test_value;
void create() { test_value = 42; }
int get_value() { return test_value; }
void set_value(int v) { test_value = v; }
int add(int a, int b) { return a + b; }
");

        // Create test object that uses arrow syntax
        File.WriteAllText(Path.Combine(_testMudlibPath, "obj", "caller.c"), @"
inherit ""/std/object"";
void create() { ::create(); }

int test_arrow_call(object target) {
    return target->get_value();
}

int test_arrow_with_args(object target) {
    target->set_value(100);
    return target->get_value();
}

int test_arrow_multi_args(object target) {
    return target->add(10, 20);
}
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
    public void ArrowCall_CallsFunction()
    {
        var target = _objectManager.CloneObject("/std/object");
        var caller = _objectManager.CloneObject("/obj/caller");

        var result = _interpreter.CallFunctionOnObject(caller, "test_arrow_call", new List<object> { target });
        Assert.Equal(42L, result);
    }

    [Fact]
    public void ArrowCall_WithArguments()
    {
        var target = _objectManager.CloneObject("/std/object");
        var caller = _objectManager.CloneObject("/obj/caller");

        var result = _interpreter.CallFunctionOnObject(caller, "test_arrow_with_args", new List<object> { target });
        Assert.Equal(100L, result);
    }

    [Fact]
    public void ArrowCall_MultipleArguments()
    {
        var target = _objectManager.CloneObject("/std/object");
        var caller = _objectManager.CloneObject("/obj/caller");

        var result = _interpreter.CallFunctionOnObject(caller, "test_arrow_multi_args", new List<object> { target });
        Assert.Equal(30L, result);
    }

    [Fact]
    public void ArrowCall_OnZero_ReturnsZero()
    {
        // Create object that tries to call on 0
        File.WriteAllText(Path.Combine(_testMudlibPath, "obj", "zero_caller.c"), @"
inherit ""/std/object"";
int test_zero() {
    object o;
    o = 0;
    return o->get_value();
}
");
        var caller = _objectManager.LoadObject("/obj/zero_caller");
        var result = _interpreter.CallFunctionOnObject(caller, "test_zero", new List<object>());
        Assert.Equal(0L, result);
    }
}
