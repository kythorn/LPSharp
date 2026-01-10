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
