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

    [Fact]
    public void Catch_ReturnsZeroOnSuccess()
    {
        // Create a file that uses catch with an expression that succeeds
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "catch_success.c"), @"
inherit ""/std/object"";

int test_result;

void main(string args) {
    test_result = catch(5 + 3);
}

int query_result() {
    return test_result;
}
");

        var obj = _objectManager.LoadObject("/test/catch_success");
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Call query_result to get the value
        _interpreter.ResetInstructionCount();
        var result = _interpreter.CallFunctionOnObject(obj, "query_result", new List<object>());

        // catch() returns 0 (not the expression result) on success
        Assert.Equal(0L, result);
    }

    [Fact]
    public void Catch_CatchesThrowAndReturnsValue()
    {
        // Create a file that uses catch to catch a throw()
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "catch_throw.c"), @"
inherit ""/std/object"";

string test_result;

void main(string args) {
    test_result = catch(throw(""my error""));
}

string query_result() {
    return test_result;
}
");

        var obj = _objectManager.LoadObject("/test/catch_throw");
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Call query_result to get the value
        _interpreter.ResetInstructionCount();
        var result = _interpreter.CallFunctionOnObject(obj, "query_result", new List<object>());

        // catch() returns the thrown value
        Assert.Equal("my error", result);
    }

    [Fact]
    public void Catch_CatchesRuntimeError()
    {
        // Create a file that uses catch to catch a runtime error
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "catch_error.c"), @"
inherit ""/std/object"";

mixed test_result;

void main(string args) {
    test_result = catch(unknown_func());
}

mixed query_result() {
    return test_result;
}
");

        var obj = _objectManager.LoadObject("/test/catch_error");
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Call query_result to get the value
        _interpreter.ResetInstructionCount();
        var result = _interpreter.CallFunctionOnObject(obj, "query_result", new List<object>());

        // catch() returns error string on runtime error
        Assert.IsType<string>(result);
        Assert.Contains("unknown_func", (string)result);
    }

    [Fact]
    public void UncaughtThrow_PropagatesAsException()
    {
        // Create a file with uncaught throw
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "uncaught_throw.c"), @"
inherit ""/std/object"";

void main(string args) {
    throw(""uncaught error"");
}
");

        var obj = _objectManager.LoadObject("/test/uncaught_throw");

        var ex = Assert.Throws<LpcThrowException>(() =>
        {
            _interpreter.ResetInstructionCount();
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        // Should contain the thrown message
        Assert.Contains("uncaught error", ex.Message);
    }

    [Fact]
    public void Catch_InNestedFunction()
    {
        // Create a file where throw happens in a nested function
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "catch_nested.c"), @"
inherit ""/std/object"";

mixed test_result;

void do_throw() {
    throw(""nested throw"");
}

void main(string args) {
    test_result = catch(do_throw());
}

mixed query_result() {
    return test_result;
}
");

        var obj = _objectManager.LoadObject("/test/catch_nested");
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        _interpreter.ResetInstructionCount();
        var result = _interpreter.CallFunctionOnObject(obj, "query_result", new List<object>());

        // catch() catches throw from nested function
        Assert.Equal("nested throw", result);
    }
}
