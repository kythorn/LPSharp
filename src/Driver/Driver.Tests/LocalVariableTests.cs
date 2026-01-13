using Xunit;

namespace Driver.Tests;

public class LocalVariableTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public LocalVariableTests()
    {
        // Create a temporary mudlib directory for testing
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_local_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "test"));

        // Create a simple object.c
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;

void create() {
    short_desc = ""something"";
}
");

        // Test: Basic local variable declaration and assignment
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "basic_local.c"), @"
inherit ""/std/object"";

int result;

void main(string args) {
    int i;
    i = 5;
    result = i;
}

int get_result() {
    return result;
}
");

        // Test: Local variable with initializer
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "local_init.c"), @"
inherit ""/std/object"";

int result;

void main(string args) {
    int i = 10;
    result = i;
}

int get_result() {
    return result;
}
");

        // Test: Multiple local variables
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "multi_local.c"), @"
inherit ""/std/object"";

int result;

void main(string args) {
    int a;
    int b;
    a = 1;
    b = 2;
    result = a + b;
}

int get_result() {
    return result;
}
");

        // Test: Local shadows object variable
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "shadow_local.c"), @"
inherit ""/std/object"";

int x;
int local_value;
int object_value;

void create() {
    ::create();
    x = 100;
}

void main(string args) {
    int x;
    x = 5;
    local_value = x;
}

void check_object_x() {
    object_value = x;
}

int get_local_value() {
    return local_value;
}

int get_object_value() {
    return object_value;
}
");

        // Test: For loop with local counter
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "loop_local.c"), @"
inherit ""/std/object"";

int result;

void main(string args) {
    int i;
    int sum;
    sum = 0;
    for (i = 0; i < 5; i++) {
        sum = sum + i;
    }
    result = sum;
}

int get_result() {
    return result;
}
");

        // Test: String local variable
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "string_local.c"), @"
inherit ""/std/object"";

string result;

void main(string args) {
    string s;
    s = ""hello"";
    result = s;
}

string get_result() {
    return result;
}
");

        // Test: Local variable default values
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "default_local.c"), @"
inherit ""/std/object"";

int int_result;
string str_result;

void main(string args) {
    int i;
    string s;
    int_result = i;
    str_result = s;
}

int get_int_result() {
    return int_result;
}

string get_str_result() {
    return str_result;
}
");

        // Test: sscanf with last %s capturing rest of string
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "sscanf_last_s.c"), @"
inherit ""/std/object"";

string item_result;
string container_result;
int match_count;

void main(string args) {
    string item;
    string container;
    match_count = sscanf(""all from corpse 3"", ""%s from %s"", item, container);
    item_result = item;
    container_result = container;
}
");

        // Test: sscanf with %s%d pattern
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "sscanf_s_d.c"), @"
inherit ""/std/object"";

string name_result;
int num_result;
int match_count;

void main(string args) {
    string name;
    int num;
    match_count = sscanf(""sword 3"", ""%s%d"", name, num);
    name_result = name;
    num_result = num;
}
");

        // Test: sscanf with complex pattern
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "sscanf_complex.c"), @"
inherit ""/std/object"";

string item_result;
string container_result;
int match_count;

void main(string args) {
    string item;
    string container;
    match_count = sscanf(""sword 2 from bag 3"", ""%s from %s"", item, container);
    item_result = item;
    container_result = container;
}
");

        _objectManager = new ObjectManager(_testMudlibPath);
        _objectManager.InitializeInterpreter();
        _interpreter = new ObjectInterpreter(_objectManager);
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_testMudlibPath))
        {
            try
            {
                Directory.Delete(_testMudlibPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void BasicLocalVariable_DeclarationAndAssignment()
    {
        var obj = _objectManager.LoadObject("/test/basic_local");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var result = obj.GetVariable("result");
        Assert.Equal(5L, result);
    }

    [Fact]
    public void LocalVariable_WithInitializer()
    {
        var obj = _objectManager.LoadObject("/test/local_init");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var result = obj.GetVariable("result");
        Assert.Equal(10L, result);
    }

    [Fact]
    public void MultipleLocalVariables()
    {
        var obj = _objectManager.LoadObject("/test/multi_local");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var result = obj.GetVariable("result");
        Assert.Equal(3L, result);
    }

    [Fact]
    public void LocalVariable_ShadowsObjectVariable()
    {
        var obj = _objectManager.LoadObject("/test/shadow_local");

        // Run main which uses local x = 5
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        var localValue = obj.GetVariable("local_value");
        Assert.Equal(5L, localValue);

        // Check that object-level x is still 100
        _interpreter.CallFunctionOnObject(obj, "check_object_x", new List<object>());
        var objectValue = obj.GetVariable("object_value");
        Assert.Equal(100L, objectValue);
    }

    [Fact]
    public void ForLoop_WithLocalCounter()
    {
        var obj = _objectManager.LoadObject("/test/loop_local");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var result = obj.GetVariable("result");
        Assert.Equal(10L, result); // 0 + 1 + 2 + 3 + 4 = 10
    }

    [Fact]
    public void StringLocalVariable()
    {
        var obj = _objectManager.LoadObject("/test/string_local");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var result = obj.GetVariable("result");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void LocalVariable_DefaultValues()
    {
        var obj = _objectManager.LoadObject("/test/default_local");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var intResult = obj.GetVariable("int_result");
        var strResult = obj.GetVariable("str_result");

        Assert.Equal(0L, intResult);
        Assert.Equal("", strResult);
    }

    [Fact]
    public void Sscanf_LastSpecifierCapturesRestOfString()
    {
        // Tests that "%s from %s" with "all from corpse 3" captures "corpse 3" not just "corpse"
        var obj = _objectManager.LoadObject("/test/sscanf_last_s");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var matchCount = obj.GetVariable("match_count");
        var item = obj.GetVariable("item_result");
        var container = obj.GetVariable("container_result");

        Assert.Equal(2, Convert.ToInt32(matchCount));
        Assert.Equal("all", item);
        Assert.Equal("corpse 3", container);  // Should capture the full rest of string
    }

    [Fact]
    public void Sscanf_PercentSPercentD_StopsAtDigit()
    {
        // Tests that "%s%d" properly stops %s before the digit
        var obj = _objectManager.LoadObject("/test/sscanf_s_d");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var matchCount = obj.GetVariable("match_count");
        var name = obj.GetVariable("name_result");
        var num = obj.GetVariable("num_result");

        Assert.Equal(2, Convert.ToInt32(matchCount));
        Assert.Equal("sword ", name);  // Captures up to (but not including) the digit
        Assert.Equal(3, Convert.ToInt32(num));
    }

    [Fact]
    public void Sscanf_ComplexPattern_CapturesMultiWordParts()
    {
        // Tests that "%s from %s" with "sword 2 from bag 3" captures both multi-word parts
        var obj = _objectManager.LoadObject("/test/sscanf_complex");
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        var matchCount = obj.GetVariable("match_count");
        var item = obj.GetVariable("item_result");
        var container = obj.GetVariable("container_result");

        Assert.Equal(2, Convert.ToInt32(matchCount));
        Assert.Equal("sword 2", item);
        Assert.Equal("bag 3", container);
    }
}
