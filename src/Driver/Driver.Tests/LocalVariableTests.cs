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
}
