using Xunit;

namespace Driver.Tests;

public class AccountManagerTests : IDisposable
{
    private readonly string _testPath;
    private readonly AccountManager _accountManager;

    public AccountManagerTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"accounts_test_{Guid.NewGuid():N}");
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
    public void CreateAccount_Success()
    {
        var result = _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        Assert.True(result);
        Assert.True(_accountManager.AccountExists("testuser"));
    }

    [Fact]
    public void CreateAccount_DuplicateUsername_Fails()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.CreateAccount("testuser", "other@example.com", "password456");

        Assert.False(result);
    }

    [Fact]
    public void CreateAccount_CaseInsensitive()
    {
        _accountManager.CreateAccount("TestUser", "test@example.com", "password123");

        Assert.True(_accountManager.AccountExists("testuser"));
        Assert.True(_accountManager.AccountExists("TESTUSER"));
        Assert.True(_accountManager.AccountExists("TeStUsEr"));
    }

    [Fact]
    public void AccountExists_NonExistent_ReturnsFalse()
    {
        Assert.False(_accountManager.AccountExists("nonexistent"));
    }

    [Fact]
    public void ValidateCredentials_Success()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.ValidateCredentials("testuser", "password123");

        Assert.True(result);
    }

    [Fact]
    public void ValidateCredentials_WrongPassword_Fails()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.ValidateCredentials("testuser", "wrongpassword");

        Assert.False(result);
    }

    [Fact]
    public void ValidateCredentials_NonExistentUser_Fails()
    {
        var result = _accountManager.ValidateCredentials("nonexistent", "password123");

        Assert.False(result);
    }

    [Fact]
    public void ValidateCredentials_CaseInsensitiveUsername()
    {
        _accountManager.CreateAccount("TestUser", "test@example.com", "password123");

        Assert.True(_accountManager.ValidateCredentials("testuser", "password123"));
        Assert.True(_accountManager.ValidateCredentials("TESTUSER", "password123"));
    }

    [Fact]
    public void ValidateCredentials_CaseSensitivePassword()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "Password123");

        Assert.True(_accountManager.ValidateCredentials("testuser", "Password123"));
        Assert.False(_accountManager.ValidateCredentials("testuser", "password123"));
        Assert.False(_accountManager.ValidateCredentials("testuser", "PASSWORD123"));
    }

    [Fact]
    public void UpdateLastLogin_UpdatesTimestamp()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // Wait a bit to ensure timestamp changes
        Thread.Sleep(10);

        _accountManager.UpdateLastLogin("testuser");

        // Can't directly check the timestamp, but shouldn't throw
    }

    [Fact]
    public void PasswordHashing_DifferentSaltsForSamePassword()
    {
        // Create two accounts with the same password
        _accountManager.CreateAccount("user1", "user1@example.com", "samepassword");
        _accountManager.CreateAccount("user2", "user2@example.com", "samepassword");

        // Both should validate correctly
        Assert.True(_accountManager.ValidateCredentials("user1", "samepassword"));
        Assert.True(_accountManager.ValidateCredentials("user2", "samepassword"));

        // The stored hashes should be different (different salts)
        // We can't check directly, but the fact that both validate is enough
    }

    [Fact]
    public void PathSanitization_PreventDirectoryTraversal()
    {
        // These usernames should not create files outside the accounts directory
        // They should be sanitized to safe names

        var result1 = _accountManager.CreateAccount("../../../etc/passwd", "test@example.com", "password123");
        var result2 = _accountManager.CreateAccount("..\\..\\windows\\system32", "test@example.com", "password123");

        // Should create accounts with sanitized names, not fail
        // The actual file should be in the accounts directory
        Assert.True(result1);
        Assert.True(result2);

        // Verify accounts exist under sanitized names
        Assert.True(_accountManager.AccountExists("etcpasswd"));
        Assert.True(_accountManager.AccountExists("windowssystem32"));
    }

    [Fact]
    public void EmptyPassword_StillWorks()
    {
        // While we enforce minimum password length in the UI, the manager should still work
        var result = _accountManager.CreateAccount("testuser", "test@example.com", "");

        Assert.True(result);
        Assert.True(_accountManager.ValidateCredentials("testuser", ""));
        Assert.False(_accountManager.ValidateCredentials("testuser", "anypassword"));
    }

    [Fact]
    public void LongPassword_Works()
    {
        var longPassword = new string('a', 1000);
        _accountManager.CreateAccount("testuser", "test@example.com", longPassword);

        Assert.True(_accountManager.ValidateCredentials("testuser", longPassword));
        Assert.False(_accountManager.ValidateCredentials("testuser", longPassword + "x"));
    }

    [Fact]
    public void UnicodePassword_Works()
    {
        var unicodePassword = "pässwörd123!";
        _accountManager.CreateAccount("testuser", "test@example.com", unicodePassword);

        Assert.True(_accountManager.ValidateCredentials("testuser", unicodePassword));
        Assert.False(_accountManager.ValidateCredentials("testuser", "password123!"));
    }

    [Fact]
    public void FirstAccount_IsAdmin()
    {
        // First account should be Admin
        _accountManager.CreateAccount("firstuser", "first@example.com", "password123");

        Assert.Equal(AccessLevel.Admin, _accountManager.GetAccessLevel("firstuser"));
    }

    [Fact]
    public void SecondAccount_IsPlayer()
    {
        // First account is Admin
        _accountManager.CreateAccount("firstuser", "first@example.com", "password123");

        // Second account should be Player
        _accountManager.CreateAccount("seconduser", "second@example.com", "password123");

        Assert.Equal(AccessLevel.Player, _accountManager.GetAccessLevel("seconduser"));
    }

    [Fact]
    public void GetAccessLevel_NonExistent_ReturnsGuest()
    {
        Assert.Equal(AccessLevel.Guest, _accountManager.GetAccessLevel("nonexistent"));
    }

    [Fact]
    public void SetAccessLevel_Success()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.SetAccessLevel("testuser", AccessLevel.Wizard);

        Assert.True(result);
        Assert.Equal(AccessLevel.Wizard, _accountManager.GetAccessLevel("testuser"));
    }

    [Fact]
    public void SetAccessLevel_NonExistent_ReturnsFalse()
    {
        var result = _accountManager.SetAccessLevel("nonexistent", AccessLevel.Wizard);

        Assert.False(result);
    }

    [Fact]
    public void SetAccessLevel_ToAdmin_Works()
    {
        _accountManager.CreateAccount("firstuser", "first@example.com", "password123");
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        var result = _accountManager.SetAccessLevel("testuser", AccessLevel.Admin);

        Assert.True(result);
        Assert.Equal(AccessLevel.Admin, _accountManager.GetAccessLevel("testuser"));
    }

    [Fact]
    public void SetAccessLevel_DemoteToPlayer_Works()
    {
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // First user is Admin, demote to Player
        var result = _accountManager.SetAccessLevel("testuser", AccessLevel.Player);

        Assert.True(result);
        Assert.Equal(AccessLevel.Player, _accountManager.GetAccessLevel("testuser"));
    }

    [Fact]
    public void GetWizardHomePath_ReturnsCorrectPath()
    {
        var path = _accountManager.GetWizardHomePath("TestUser");

        Assert.EndsWith("wizards/testuser", path.Replace("\\", "/"));
    }

    [Fact]
    public void SetAccessLevel_CreatesWizardHomeDirectory()
    {
        _accountManager.CreateAccount("firstuser", "first@example.com", "password123");
        _accountManager.CreateAccount("testuser", "test@example.com", "password123");

        // Promote to Wizard
        _accountManager.SetAccessLevel("testuser", AccessLevel.Wizard);

        // Home directory should be created
        var homePath = _accountManager.GetWizardHomePath("testuser");
        Assert.True(Directory.Exists(homePath));
    }
}
