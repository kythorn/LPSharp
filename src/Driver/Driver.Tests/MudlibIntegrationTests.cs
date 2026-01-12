using Xunit;
using System.IO;

namespace Driver.Tests;

/// <summary>
/// Integration tests that validate the actual mudlib files can be compiled.
/// These tests ensure that the real mudlib directory contains valid LPC code.
/// </summary>
public class MudlibIntegrationTests
{
    private static string GetMudlibPath()
    {
        // Find the mudlib directory relative to the test assembly
        // The tests run from src/Driver/Driver.Tests/bin/Debug/net9.0/
        // Mudlib is at the repo root: /mudlib
        var currentDir = Directory.GetCurrentDirectory();

        // Try various relative paths to find mudlib
        var candidates = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "..", "mudlib"),
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "mudlib"),
            Path.Combine(currentDir, "mudlib"),
            "mudlib"
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath) && Directory.Exists(Path.Combine(fullPath, "std")))
            {
                return fullPath;
            }
        }

        // Fallback: use environment variable if set
        var envPath = Environment.GetEnvironmentVariable("MUDLIB_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            return envPath;
        }

        throw new DirectoryNotFoundException(
            $"Could not find mudlib directory. Current dir: {currentDir}. " +
            "Set MUDLIB_PATH environment variable to the mudlib directory.");
    }

    [Fact]
    public void AllStdFiles_ShouldCompile()
    {
        var mudlibPath = GetMudlibPath();
        var stdPath = Path.Combine(mudlibPath, "std");
        var files = Directory.GetFiles(stdPath, "*.c");

        Assert.NotEmpty(files);

        var om = new ObjectManager(mudlibPath);
        om.InitializeInterpreter();

        var errors = new List<string>();

        foreach (var file in files)
        {
            var relativePath = "/" + Path.GetRelativePath(mudlibPath, file)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(".c", "");

            try
            {
                var obj = om.LoadObject(relativePath);
                Assert.NotNull(obj);
            }
            catch (Exception ex)
            {
                errors.Add($"{relativePath}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            Assert.Fail($"Failed to compile {errors.Count} file(s):\n" + string.Join("\n", errors));
        }
    }

    [Fact]
    public void AllCommandFiles_ShouldCompile()
    {
        var mudlibPath = GetMudlibPath();
        var cmdsPath = Path.Combine(mudlibPath, "cmds");

        if (!Directory.Exists(cmdsPath))
        {
            return; // Skip if no cmds directory
        }

        var files = Directory.GetFiles(cmdsPath, "*.c", SearchOption.AllDirectories);

        var om = new ObjectManager(mudlibPath);
        om.InitializeInterpreter();

        var errors = new List<string>();

        foreach (var file in files)
        {
            var relativePath = "/" + Path.GetRelativePath(mudlibPath, file)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(".c", "");

            try
            {
                var obj = om.LoadObject(relativePath);
                Assert.NotNull(obj);
            }
            catch (Exception ex)
            {
                errors.Add($"{relativePath}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            Assert.Fail($"Failed to compile {errors.Count} command file(s):\n" + string.Join("\n", errors));
        }
    }

    [Fact]
    public void AllWorldFiles_ShouldCompile()
    {
        var mudlibPath = GetMudlibPath();
        var worldPath = Path.Combine(mudlibPath, "world");

        if (!Directory.Exists(worldPath))
        {
            return; // Skip if no world directory
        }

        var files = Directory.GetFiles(worldPath, "*.c", SearchOption.AllDirectories);

        var om = new ObjectManager(mudlibPath);
        om.InitializeInterpreter();

        var errors = new List<string>();

        foreach (var file in files)
        {
            var relativePath = "/" + Path.GetRelativePath(mudlibPath, file)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(".c", "");

            try
            {
                var obj = om.LoadObject(relativePath);
                Assert.NotNull(obj);
            }
            catch (Exception ex)
            {
                errors.Add($"{relativePath}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            Assert.Fail($"Failed to compile {errors.Count} world file(s):\n" + string.Join("\n", errors));
        }
    }

    [Fact]
    public void PlayerObject_ShouldCloneSuccessfully()
    {
        // This is the specific scenario that broke login
        var mudlibPath = GetMudlibPath();
        var om = new ObjectManager(mudlibPath);
        om.InitializeInterpreter();

        // This should not throw - it's what the login flow does
        var player = om.CloneObject("/std/player");

        Assert.NotNull(player);
        Assert.False(player.IsBlueprint);

        // Verify we can call functions on the player
        var interpreter = om.Interpreter;
        interpreter!.CallFunctionOnObject(player, "set_name", new List<object> { "TestPlayer" });

        var name = player.GetVariable("player_name");
        Assert.Equal("TestPlayer", name);
    }
}
