using Xunit;

namespace Driver.Tests;

public class AliasTests : IDisposable
{
    private readonly string _testPath;
    private readonly AccountManager _accountManager;

    public AliasTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"alias_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPath);
        _accountManager = new AccountManager(_testPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            try
            {
                Directory.Delete(_testPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void NewAccount_HasDefaultAliases()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var aliases = _accountManager.GetAliases("testuser");

        // Should have direction aliases
        Assert.Contains("n", aliases.Keys);
        Assert.Contains("north", aliases.Keys);
        Assert.Equal("go north", aliases["n"]);
        Assert.Equal("go north", aliases["north"]);

        // Should have other common aliases
        Assert.Contains("l", aliases.Keys);
        Assert.Equal("look", aliases["l"]);

        Assert.Contains("i", aliases.Keys);
        Assert.Equal("inventory", aliases["i"]);
    }

    [Fact]
    public void SetAlias_AddsNewAlias()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.SetAlias("testuser", "k", "kill");

        Assert.True(result);

        var aliases = _accountManager.GetAliases("testuser");
        Assert.Contains("k", aliases.Keys);
        Assert.Equal("kill", aliases["k"]);
    }

    [Fact]
    public void SetAlias_UpdatesExistingAlias()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // Default 'n' is "go north", change it
        _accountManager.SetAlias("testuser", "n", "go northeast");

        var aliases = _accountManager.GetAliases("testuser");
        Assert.Equal("go northeast", aliases["n"]);
    }

    [Fact]
    public void RemoveAlias_RemovesExisting()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.RemoveAlias("testuser", "n");

        Assert.True(result);

        var aliases = _accountManager.GetAliases("testuser");
        Assert.DoesNotContain("n", aliases.Keys);
    }

    [Fact]
    public void RemoveAlias_NonExistent_ReturnsFalse()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.RemoveAlias("testuser", "nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void ResetAliases_RestoresDefaults()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // Modify some aliases
        _accountManager.SetAlias("testuser", "n", "something else");
        _accountManager.RemoveAlias("testuser", "s");
        _accountManager.SetAlias("testuser", "custom", "custom command");

        // Reset
        var result = _accountManager.ResetAliases("testuser");

        Assert.True(result);

        var aliases = _accountManager.GetAliases("testuser");

        // Should be back to defaults
        Assert.Equal("go north", aliases["n"]);
        Assert.Contains("s", aliases.Keys);
        Assert.DoesNotContain("custom", aliases.Keys);
    }

    [Fact]
    public void GetAliases_NonExistentUser_ReturnsDefaults()
    {
        var aliases = _accountManager.GetAliases("nonexistent");

        // Should still get defaults
        Assert.Contains("n", aliases.Keys);
        Assert.Contains("l", aliases.Keys);
    }

    [Fact]
    public void SetAlias_NonExistentUser_ReturnsFalse()
    {
        var result = _accountManager.SetAlias("nonexistent", "test", "command");

        Assert.False(result);
    }

    [Fact]
    public void GetDefaultAliases_ContainsAllDirections()
    {
        var defaults = AccountManager.GetDefaultAliases();

        // Short directions
        Assert.Contains("n", defaults.Keys);
        Assert.Contains("s", defaults.Keys);
        Assert.Contains("e", defaults.Keys);
        Assert.Contains("w", defaults.Keys);
        Assert.Contains("u", defaults.Keys);
        Assert.Contains("d", defaults.Keys);
        Assert.Contains("ne", defaults.Keys);
        Assert.Contains("nw", defaults.Keys);
        Assert.Contains("se", defaults.Keys);
        Assert.Contains("sw", defaults.Keys);

        // Long directions
        Assert.Contains("north", defaults.Keys);
        Assert.Contains("south", defaults.Keys);
        Assert.Contains("east", defaults.Keys);
        Assert.Contains("west", defaults.Keys);
        Assert.Contains("up", defaults.Keys);
        Assert.Contains("down", defaults.Keys);
        Assert.Contains("northeast", defaults.Keys);
        Assert.Contains("northwest", defaults.Keys);
        Assert.Contains("southeast", defaults.Keys);
        Assert.Contains("southwest", defaults.Keys);
    }

    [Fact]
    public void Aliases_AreCaseInsensitive()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // Set with mixed case
        _accountManager.SetAlias("testuser", "MYCMD", "my command");

        var aliases = _accountManager.GetAliases("testuser");

        // Should find with any case
        Assert.True(aliases.ContainsKey("mycmd"));
        Assert.True(aliases.ContainsKey("MYCMD"));
        Assert.True(aliases.ContainsKey("MyCmd"));
    }

    [Fact]
    public void Aliases_PersistAcrossReload()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");
        _accountManager.SetAlias("testuser", "custom", "do something");

        // Create new AccountManager instance (simulates restart)
        var newManager = new AccountManager(_testPath);
        var aliases = newManager.GetAliases("testuser");

        Assert.Contains("custom", aliases.Keys);
        Assert.Equal("do something", aliases["custom"]);
    }

    #region Security Tests

    [Fact]
    public void IsProtectedCommand_ReturnsTrueForProtected()
    {
        Assert.True(GameLoop.IsProtectedCommand("quit"));
        Assert.True(GameLoop.IsProtectedCommand("alias"));
        Assert.True(GameLoop.IsProtectedCommand("password"));
        Assert.True(GameLoop.IsProtectedCommand("save"));
        Assert.True(GameLoop.IsProtectedCommand("who"));
        Assert.True(GameLoop.IsProtectedCommand("unalias"));
    }

    [Fact]
    public void IsProtectedCommand_ReturnsFalseForNonProtected()
    {
        Assert.False(GameLoop.IsProtectedCommand("look"));
        Assert.False(GameLoop.IsProtectedCommand("go"));
        Assert.False(GameLoop.IsProtectedCommand("inventory"));
        Assert.False(GameLoop.IsProtectedCommand("n"));
        Assert.False(GameLoop.IsProtectedCommand("custom"));
    }

    [Fact]
    public void IsProtectedCommand_IsCaseInsensitive()
    {
        Assert.True(GameLoop.IsProtectedCommand("QUIT"));
        Assert.True(GameLoop.IsProtectedCommand("Quit"));
        Assert.True(GameLoop.IsProtectedCommand("ALIAS"));
    }

    #endregion
}
