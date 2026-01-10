using Xunit;
using Xunit.Abstractions;

namespace Driver.Tests;

public class RoomLoadingTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ITestOutputHelper _output;

    public RoomLoadingTests(ITestOutputHelper output)
    {
        _output = output;
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_load_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "room"));
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
    public void SimpleObject_CanBeLoaded()
    {
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { short_desc = ""something""; }
");

        var objectManager = new ObjectManager(_testMudlibPath);
        objectManager.InitializeInterpreter();

        var obj = objectManager.LoadObject("/std/object");
        Assert.NotNull(obj);
        Assert.Equal("something", obj.GetVariable("short_desc"));
    }

    [Fact]
    public void ObjectWithFunction_CanBeLoaded()
    {
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { set_short(""something""); }
void set_short(string s) { short_desc = s; }
");

        var objectManager = new ObjectManager(_testMudlibPath);
        objectManager.InitializeInterpreter();

        var obj = objectManager.LoadObject("/std/object");
        Assert.NotNull(obj);
        Assert.Equal("something", obj.GetVariable("short_desc"));
    }

    [Fact]
    public void InheritedObject_CanBeLoaded()
    {
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { short_desc = ""base""; }
void set_short(string s) { short_desc = s; }
");

        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "room.c"), @"
inherit ""/std/object"";
void create() { ::create(); set_short(""room""); }
");

        var objectManager = new ObjectManager(_testMudlibPath);
        objectManager.InitializeInterpreter();

        var obj = objectManager.LoadObject("/std/room");
        Assert.NotNull(obj);
        Assert.Equal("room", obj.GetVariable("short_desc"));
    }

    [Fact]
    public void DoubleInheritedObject_CanBeLoaded()
    {
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;
void create() { short_desc = ""base""; }
void set_short(string s) { short_desc = s; }
");

        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "room.c"), @"
inherit ""/std/object"";
string long_desc;
void create() { ::create(); long_desc = ""desc""; }
void set_long(string s) { long_desc = s; }
");

        File.WriteAllText(Path.Combine(_testMudlibPath, "room", "test.c"), @"
inherit ""/std/room"";
void create() { ::create(); set_short(""test room""); set_long(""A test room.""); }
");

        var objectManager = new ObjectManager(_testMudlibPath);
        objectManager.InitializeInterpreter();

        var obj = objectManager.LoadObject("/room/test");
        Assert.NotNull(obj);
        Assert.Equal("test room", obj.GetVariable("short_desc"));
        Assert.Equal("A test room.", obj.GetVariable("long_desc"));
    }
}
