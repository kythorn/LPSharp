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

    // PBKDF2 parameters
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public AccountManager(string mudlibPath)
    {
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

        var account = new AccountData
        {
            Username = username.ToLowerInvariant(),
            Email = email,
            PasswordHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash),
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            LoginCount = 1
        };

        SaveAccount(account);
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
}
