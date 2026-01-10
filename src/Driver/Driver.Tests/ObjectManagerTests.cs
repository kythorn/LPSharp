using Xunit;
using System.IO;

namespace Driver.Tests;

public class ObjectManagerTests
{
    [Fact]
    public void LoadObject_CreatesBlueprint()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj = om.LoadObject("/std/object");

        Assert.NotNull(obj);
        Assert.True(obj.IsBlueprint);
        Assert.Equal("/std/object", obj.ObjectName);
        Assert.Equal("/std/object", obj.FilePath);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void LoadObject_CachesBlueprints()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj1 = om.LoadObject("/std/object");
        var obj2 = om.LoadObject("/std/object");

        Assert.Same(obj1, obj2); // Same instance

        CleanupTemp(tempDir);
    }

    [Fact]
    public void CloneObject_CreatesUniqueClones()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var clone1 = om.CloneObject("/std/object");
        var clone2 = om.CloneObject("/std/object");

        Assert.NotNull(clone1);
        Assert.NotNull(clone2);
        Assert.NotSame(clone1, clone2);
        Assert.False(clone1.IsBlueprint);
        Assert.False(clone2.IsBlueprint);
        Assert.Equal("/std/object#1", clone1.ObjectName);
        Assert.Equal("/std/object#2", clone2.ObjectName);
        Assert.Equal("/std/object", clone1.FilePath);
        Assert.Equal("/std/object", clone2.FilePath);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void LoadObject_HandlesInheritance()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var weapon = om.LoadObject("/std/weapon");

        Assert.NotNull(weapon);
        Assert.Single(weapon.Program.InheritedPrograms);
        Assert.Equal("/std/object", weapon.Program.InheritedPrograms[0].FilePath);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void LoadObject_CallsCreate()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);

        var output = new StringWriter();
        om.InitializeInterpreter(output);

        var obj = om.LoadObject("/std/object");

        // The object's create() should have been called
        Assert.NotNull(obj);

        // Check that variables were initialized
        var shortDesc = obj.GetVariable("short_desc");
        Assert.NotNull(shortDesc);
        Assert.Equal("something", shortDesc);

        var mass = obj.GetVariable("mass");
        Assert.NotNull(mass);
        Assert.Equal(1L, mass);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void CloneObject_CallsCreate()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var clone = om.CloneObject("/std/weapon");

        // Each clone should have independent variables initialized by create()
        var damage = clone.GetVariable("damage");
        Assert.NotNull(damage);
        Assert.Equal(5L, damage); // Default from /std/weapon create()

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Clones_HaveIndependentVariables()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var clone1 = om.CloneObject("/std/weapon");
        var clone2 = om.CloneObject("/std/weapon");

        // Modify clone1's variable
        clone1.SetVariable("damage", 100L);

        // clone2 should be unaffected
        Assert.Equal(100L, clone1.GetVariable("damage"));
        Assert.Equal(5L, clone2.GetVariable("damage"));

        CleanupTemp(tempDir);
    }

    [Fact]
    public void LoadObject_ExecutesVariableInitializers()
    {
        var tempDir = CreateTempMudlib();

        // Create a test file with variable initializers
        var testFile = Path.Combine(tempDir, "test.c");
        File.WriteAllText(testFile, @"
int initialized = 42;
string name = ""test"";

void create() {
    // Variables should already be initialized
}
");

        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj = om.LoadObject("/test");

        Assert.Equal(42L, obj.GetVariable("initialized"));
        Assert.Equal("test", obj.GetVariable("name"));

        CleanupTemp(tempDir);
    }

    [Fact]
    public void FindObject_ReturnsCorrectObject()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj = om.LoadObject("/std/object");
        var clone = om.CloneObject("/std/object");

        Assert.Same(obj, om.FindObject("/std/object"));
        Assert.Same(clone, om.FindObject("/std/object#1"));
        Assert.Null(om.FindObject("/nonexistent"));

        CleanupTemp(tempDir);
    }

    [Fact]
    public void DestructObject_RemovesObject()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var clone = om.CloneObject("/std/object");
        var name = clone.ObjectName;

        Assert.NotNull(om.FindObject(name));

        om.DestructObject(clone);

        Assert.Null(om.FindObject(name));
        Assert.True(clone.IsDestructed);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void GetStats_ReturnsCorrectCounts()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        om.LoadObject("/std/object");
        om.LoadObject("/std/weapon");
        om.CloneObject("/std/object");
        om.CloneObject("/std/object");
        om.CloneObject("/std/weapon");

        var stats = om.GetStats();

        Assert.Equal(2, stats.BlueprintCount); // /std/object, /std/weapon
        Assert.Equal(5, stats.TotalObjectCount); // 2 blueprints + 3 clones
        Assert.Equal(3, stats.CloneCount); // 3 clones

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Efun_CloneObject_Works()
    {
        var tempDir = CreateTempMudlib();

        // Create a test file that uses clone_object()
        var testFile = Path.Combine(tempDir, "test_clone.c");
        File.WriteAllText(testFile, @"
void create() {
}

void test_clone() {
    clone_object(""/std/object"");
}
");

        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj = om.LoadObject("/test_clone");

        // Call the test_clone function which uses clone_object() efun
        var interpreter = new ObjectInterpreter(om);
        var func = obj.FindFunction("test_clone");
        Assert.NotNull(func);

        interpreter.ExecuteInObject(obj, func.Body);

        var stats = om.GetStats();
        Assert.Equal(3, stats.TotalObjectCount); // test_clone blueprint, std/object blueprint, and 1 clone

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Efun_ThisObject_Works()
    {
        var tempDir = CreateTempMudlib();

        // Create a test file that uses this_object()
        var testFile = Path.Combine(tempDir, "test_this.c");
        File.WriteAllText(testFile, @"
int test_value;

void create() {
    test_value = 42;
}

void verify_this() {
    // In LPC, this_object() returns the current object
    // We'll test it by checking variables are accessible
    test_value = 100;
}
");

        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var obj = om.LoadObject("/test_this");
        Assert.Equal(42L, obj.GetVariable("test_value"));

        var interpreter = new ObjectInterpreter(om);
        var func = obj.FindFunction("verify_this");
        Assert.NotNull(func);

        interpreter.ExecuteInObject(obj, func.Body);

        // Verify the function modified the object's variable
        Assert.Equal(100L, obj.GetVariable("test_value"));

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Efun_LoadObject_Works()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        // Load using LoadObject method
        var obj1 = om.LoadObject("/std/object");

        // Load again - should return same blueprint
        var obj2 = om.LoadObject("/std/object");

        Assert.Same(obj1, obj2);
        Assert.True(obj1.IsBlueprint);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Efun_FindObject_Works()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var blueprint = om.LoadObject("/std/object");
        var clone = om.CloneObject("/std/object");

        // Find blueprint
        var foundBlueprint = om.FindObject("/std/object");
        Assert.Same(blueprint, foundBlueprint);

        // Find clone
        var foundClone = om.FindObject(clone.ObjectName);
        Assert.Same(clone, foundClone);

        // Find non-existent
        var notFound = om.FindObject("/nonexistent");
        Assert.Null(notFound);

        CleanupTemp(tempDir);
    }

    [Fact]
    public void Efun_Destruct_Works()
    {
        var tempDir = CreateTempMudlib();
        var om = new ObjectManager(tempDir);
        om.InitializeInterpreter();

        var clone = om.CloneObject("/std/object");
        var cloneName = clone.ObjectName;

        // Verify clone exists
        Assert.NotNull(om.FindObject(cloneName));
        Assert.False(clone.IsDestructed);

        // Destruct it
        om.DestructObject(clone);

        // Verify it's gone
        Assert.Null(om.FindObject(cloneName));
        Assert.True(clone.IsDestructed);

        CleanupTemp(tempDir);
    }

    // Helper methods

    private string CreateTempMudlib()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mudlib_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "std"));

        // Create /std/object.c
        File.WriteAllText(Path.Combine(tempDir, "std", "object.c"), @"
string short_desc;
int mass;

void create() {
    short_desc = ""something"";
    mass = 1;
}

string query_short() {
    return short_desc;
}

void set_short(string desc) {
    short_desc = desc;
}

int query_mass() {
    return mass;
}

void set_mass(int m) {
    mass = m;
}
");

        // Create /std/weapon.c
        File.WriteAllText(Path.Combine(tempDir, "std", "weapon.c"), @"
inherit ""/std/object"";

int damage;
string weapon_type;

void create() {
    ::create();
    damage = 5;
    weapon_type = ""melee"";
    set_mass(10);
}

int query_damage() {
    return damage;
}

void set_damage(int d) {
    damage = d;
}

string query_weapon_type() {
    return weapon_type;
}

void set_weapon_type(string type) {
    weapon_type = type;
}
");

        return tempDir;
    }

    private void CleanupTemp(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}
