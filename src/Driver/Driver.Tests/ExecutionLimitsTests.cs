using Xunit;

namespace Driver.Tests;

public class ExecutionLimitsTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly ObjectInterpreter _interpreter;

    public ExecutionLimitsTests()
    {
        // Create a temporary mudlib directory for testing
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_limits_test_{Guid.NewGuid():N}");
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

        // Create a test file with an infinite loop
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "infinite_loop.c"), @"
inherit ""/std/object"";

void main(string args) {
    while (1) {
        // Infinite loop
    }
}
");

        // Create a test file with infinite recursion
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "infinite_recursion.c"), @"
inherit ""/std/object"";

void recurse() {
    recurse();
}

void main(string args) {
    recurse();
}
");

        // Create a test file that does a lot of work but finishes
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "heavy_work.c"), @"
inherit ""/std/object"";

int count;
int i;

void main(string args) {
    count = 0;
    for (i = 0; i < 1000; i++) {
        count = count + 1;
    }
}

int get_count() {
    return count;
}
");

        // Create a test file with deep but finite recursion
        File.WriteAllText(Path.Combine(_testMudlibPath, "test", "deep_recursion.c"), @"
inherit ""/std/object"";

int sum;

void recurse(int depth) {
    if (depth <= 0) {
        return;
    }
    sum = sum + 1;
    recurse(depth - 1);
}

void main(string args) {
    sum = 0;
    recurse(50);
}

int get_sum() {
    return sum;
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
    public void InfiniteLoop_ThrowsExecutionLimitException()
    {
        // Set a low limit for faster testing
        _interpreter.MaxInstructions = 10_000;

        var obj = _objectManager.LoadObject("/test/infinite_loop");
        var mainFunc = obj.FindFunction("main");
        Assert.NotNull(mainFunc);

        var ex = Assert.Throws<ExecutionLimitException>(() =>
        {
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        Assert.Contains("Execution limit exceeded", ex.Message);
        Assert.Contains("infinite loop", ex.Message);
    }

    [Fact]
    public void InfiniteRecursion_ThrowsExecutionLimitException()
    {
        _interpreter.MaxRecursionDepth = 50;

        var obj = _objectManager.LoadObject("/test/infinite_recursion");
        var mainFunc = obj.FindFunction("main");
        Assert.NotNull(mainFunc);

        var ex = Assert.Throws<ExecutionLimitException>(() =>
        {
            _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });
        });

        Assert.Contains("Recursion limit exceeded", ex.Message);
        Assert.Contains("infinite recursion", ex.Message);
    }

    [Fact]
    public void HeavyWork_CompletesWithinLimits()
    {
        // Default limits should be high enough for normal work
        _interpreter.MaxInstructions = 100_000;

        var obj = _objectManager.LoadObject("/test/heavy_work");

        // Should not throw
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Verify it actually ran
        var count = obj.GetVariable("count");
        Assert.Equal(1000L, count);
    }

    [Fact]
    public void DeepButFiniteRecursion_CompletesWithinLimits()
    {
        _interpreter.MaxRecursionDepth = 100;
        _interpreter.MaxInstructions = 100_000;

        var obj = _objectManager.LoadObject("/test/deep_recursion");

        // Should not throw (50 levels is under the 100 limit)
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Verify it actually ran
        var sum = obj.GetVariable("sum");
        Assert.Equal(50L, sum);
    }

    [Fact]
    public void LimitsCanBeDisabled()
    {
        _interpreter.LimitsEnabled = false;
        _interpreter.MaxInstructions = 100; // Very low limit

        var obj = _objectManager.LoadObject("/test/heavy_work");

        // Should not throw even though we're over the instruction limit
        // because limits are disabled
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Verify it actually ran
        var count = obj.GetVariable("count");
        Assert.Equal(1000L, count);

        // Re-enable limits for cleanup
        _interpreter.LimitsEnabled = true;
    }

    [Fact]
    public void InstructionCountResets()
    {
        _interpreter.MaxInstructions = 50_000;

        var obj = _objectManager.LoadObject("/test/heavy_work");

        // Run twice with reset - both should succeed
        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        _interpreter.ResetInstructionCount();
        _interpreter.CallFunctionOnObject(obj, "main", new List<object> { "" });

        // Both runs completed
        var count = obj.GetVariable("count");
        Assert.Equal(1000L, count);
    }

    [Fact]
    public void ConfigurableLimits()
    {
        // Test that limits can be configured
        var interpreter = new ObjectInterpreter(_objectManager);

        Assert.Equal(1_000_000, interpreter.MaxInstructions);
        Assert.Equal(100, interpreter.MaxRecursionDepth);
        Assert.True(interpreter.LimitsEnabled);

        interpreter.MaxInstructions = 500_000;
        interpreter.MaxRecursionDepth = 50;
        interpreter.LimitsEnabled = false;

        Assert.Equal(500_000, interpreter.MaxInstructions);
        Assert.Equal(50, interpreter.MaxRecursionDepth);
        Assert.False(interpreter.LimitsEnabled);
    }
}
