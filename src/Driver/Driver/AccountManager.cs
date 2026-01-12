using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Driver;

/// <summary>
/// Manages player accounts including registration, authentication, and persistence.
/// Passwords are hashed using PBKDF2 with SHA256.
/// </summary>
public class AccountManager
{
    private readonly string _accountsPath;
    private readonly string _mudlibPath;

    // PBKDF2 parameters
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public AccountManager(string mudlibPath)
    {
        _mudlibPath = mudlibPath;
        _accountsPath = Path.Combine(mudlibPath, "secure", "accounts");
        Directory.CreateDirectory(_accountsPath);
    }

    /// <summary>
    /// Check if an account exists for the given username.
    /// </summary>
    public bool AccountExists(string username)
    {
        var path = GetAccountPath(username);
        return File.Exists(path);
    }

    /// <summary>
    /// Create a new account with the given credentials.
    /// First registered account automatically becomes Admin.
    /// </summary>
    /// <returns>True if account was created, false if username already exists.</returns>
    public bool CreateAccount(string username, string email, string password)
    {
        if (AccountExists(username))
        {
            return false;
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPassword(password, salt);

        // First registered user becomes Admin
        var isFirstAccount = !Directory.EnumerateFiles(_accountsPath, "*.json").Any();
        var accessLevel = isFirstAccount ? AccessLevel.Admin : AccessLevel.Player;

        var account = new AccountData
        {
            Username = username.ToLowerInvariant(),
            Email = email,
            PasswordHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash),
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            LoginCount = 1,
            AccessLevel = accessLevel
        };

        SaveAccount(account);

        // Create wizard home directory if Wizard or Admin
        if (accessLevel >= AccessLevel.Wizard)
        {
            EnsureWizardHomeDirectory(username);
        }

        return true;
    }

    /// <summary>
    /// Validate login credentials.
    /// </summary>
    /// <returns>True if credentials are valid.</returns>
    public bool ValidateCredentials(string username, string password)
    {
        var account = LoadAccount(username);
        if (account == null)
        {
            return false;
        }

        var parts = account.PasswordHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var computedHash = HashPassword(password, salt);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    /// <summary>
    /// Update the last login timestamp and increment login count.
    /// </summary>
    public void UpdateLastLogin(string username)
    {
        var account = LoadAccount(username);
        if (account == null)
        {
            return;
        }

        account.LastLogin = DateTime.UtcNow;
        account.LoginCount++;
        SaveAccount(account);
    }

    /// <summary>
    /// Get the access level for a username.
    /// </summary>
    /// <returns>The access level, or Guest if account doesn't exist.</returns>
    public AccessLevel GetAccessLevel(string username)
    {
        var account = LoadAccount(username);
        return account?.AccessLevel ?? AccessLevel.Guest;
    }

    /// <summary>
    /// Set the access level for a username.
    /// Creates wizard home directory if promoting to Wizard or higher.
    /// </summary>
    /// <returns>True if successful, false if account doesn't exist.</returns>
    public bool SetAccessLevel(string username, AccessLevel level)
    {
        var account = LoadAccount(username);
        if (account == null)
        {
            return false;
        }

        var previousLevel = account.AccessLevel;
        account.AccessLevel = level;
        SaveAccount(account);

        // Create wizard home directory when promoting to Wizard or higher
        if (level >= AccessLevel.Wizard && previousLevel < AccessLevel.Wizard)
        {
            EnsureWizardHomeDirectory(username);
        }

        return true;
    }

    /// <summary>
    /// Get the wizard home directory path for a username.
    /// </summary>
    public string GetWizardHomePath(string username)
    {
        var safe = username.ToLowerInvariant()
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");
        return Path.Combine(_mudlibPath, "wizards", safe);
    }

    /// <summary>
    /// Ensure the wizard home directory exists.
    /// </summary>
    private void EnsureWizardHomeDirectory(string username)
    {
        var homePath = GetWizardHomePath(username);
        if (!Directory.Exists(homePath))
        {
            Directory.CreateDirectory(homePath);
        }
    }

    private byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }

    private string GetAccountPath(string username)
    {
        // Sanitize to prevent directory traversal
        var safe = username.ToLowerInvariant()
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");
        return Path.Combine(_accountsPath, safe + ".json");
    }

    private AccountData? LoadAccount(string username)
    {
        var path = GetAccountPath(username);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AccountData>(json);
        }
        catch
        {
            return null;
        }
    }

    private void SaveAccount(AccountData account)
    {
        var path = GetAccountPath(account.Username);
        var json = JsonSerializer.Serialize(account, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }
}

/// <summary>
/// Account data stored in JSON files.
/// </summary>
public class AccountData
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public int LoginCount { get; set; }
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Player;
}
