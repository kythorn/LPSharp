using Xunit;

namespace Driver.Tests;

public class ErrorReportingTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public ErrorReportingTests()
    {
        // Create a temporary mudlib directory for testing
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_error_test_{Guid.NewGuid():N}");
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

        // Create a test file that calls an unknown function on a specific line
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "unknown_function.c"), @"
inherit ""/std/object"";

void main(string args) {
    int x;
    x = 5;
    unknown_function();
}
");

        // Create a test file with a division by zero
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "divide_by_zero.c"), @"
inherit ""/std/object"";

void main(string args) {
    int a;
    int b;
    int c;
    a = 10;
    b = 0;
    c = a / b;
}
");

        // Create a test file that calls a nested function that errors
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "nested_error.c"), @"
inherit ""/std/object"";

void level3() {
    unknown_func();
}

void level2() {
    level3();
}

void level1() {
    level2();
}

void main(string args) {
    level1();
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
    public void UnknownFunction_ErrorIncludesLineNumber()
    {
        var obj = _objectManager.LoadObject("/test/unknown_function");

        var ex = Assert.Throws<LpcRuntimeException>(() =>
        {
            _interpreter.ResetInstructionCount();
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        // Should include file path and line number
        Assert.Contains("/test/unknown_function", ex.Message);
        Assert.Contains("unknown_function", ex.Message);
        // Line 7 is where unknown_function() is called
        Assert.Contains(":7:", ex.Message);
    }

    [Fact]
    public void NestedError_IncludesStackTrace()
    {
        var obj = _objectManager.LoadObject("/test/nested_error");

        var ex = Assert.Throws<LpcRuntimeException>(() =>
        {
            _interpreter.ResetInstructionCount();
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        // Should include file path
        Assert.Contains("/test/nested_error", ex.Message);

        // Should include stack trace showing the call chain
        Assert.Contains("Stack trace:", ex.Message);
        Assert.Contains("level3", ex.Message);
        Assert.Contains("level2", ex.Message);
        Assert.Contains("level1", ex.Message);
        Assert.Contains("main", ex.Message);
    }

    [Fact]
    public void ExecutionLimit_ErrorIncludesLineNumber()
    {
        // Create a file with infinite loop
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "loop.c"), @"
inherit ""/std/object"";

void main(string args) {
    while (1) {
        int x;
        x = 1;
    }
}
");

        var obj = _objectManager.LoadObject("/test/loop");
        _interpreter.MaxInstructions = 1000;

        var ex = Assert.Throws<ExecutionLimitException>(() =>
        {
            _interpreter.ResetInstructionCount();
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        // Should include file path and a line number
        Assert.Contains("/test/loop", ex.Message);
        Assert.Contains(":", ex.Message);
    }
}
