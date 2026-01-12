using Xunit;

namespace Driver.Tests;

public class GameLoopTests : IDisposable
{
    private readonly string _testMudlibPath;
    private readonly ObjectManager _objectManager;
    private readonly AccountManager _accountManager;
    private readonly GameLoop _gameLoop;

    public GameLoopTests()
    {
        // Create a temporary mudlib directory for testing
        _testMudlibPath = Path.Combine(Path.GetTempPath(), $"mudlib_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testMudlibPath);
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "cmds", "std"));
        Directory.CreateDirectory(Path.Combine(_testMudlibPath, "secure", "accounts"));

        // Create a simple object.c
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "object.c"), @"
string short_desc;

void create() {
    short_desc = ""something"";
}

string query_short() {
    return short_desc;
}

void set_short(string desc) {
    short_desc = desc;
}
");

        // Create a simple player.c
        File.WriteAllText(Path.Combine(_testMudlibPath, "std", "player.c"), @"
inherit ""/std/object"";

void create() {
    ::create();
    set_short(""a player"");
}
");

        // Create a test command
        File.WriteAllText(Path.Combine(_testMudlibPath, "cmds", "std", "test.c"), @"
void main(string args) {
    write(""Test: "" + args);
}
");

        // Create a simple say command
        File.WriteAllText(Path.Combine(_testMudlibPath, "cmds", "std", "say.c"), @"
void main(string args) {
    if (args == """" || args == 0) {
        write(""Say what?"");
        return;
    }
    write(""You say: "" + args);
}
");

        _objectManager = new ObjectManager(_testMudlibPath);
        _objectManager.InitializeInterpreter();

        _accountManager = new AccountManager(_testMudlibPath);

        _gameLoop = new GameLoop(_objectManager, _accountManager);
        var interpreter = new ObjectInterpreter(_objectManager);
        _gameLoop.InitializeInterpreter(interpreter);
    }

    /// <summary>
    /// Helper to create an authenticated session for testing gameplay.
    /// Creates an account and simulates login completion.
    /// </summary>
    private void CreateAuthenticatedSession(string connectionId, string username)
    {
        // Create account
        _accountManager.CreateAccount(username, $"{username}@test.com", "password123");

        // Create session
        _gameLoop.CreatePlayerSession(connectionId);

        // Simulate login flow
        var session = _gameLoop.GetSession(connectionId);
        if (session != null)
        {
            // Queue the username
            _gameLoop.QueueCommand(connectionId, username);
            // Process login commands
            Thread.Sleep(50);
            // Queue the password
            _gameLoop.QueueCommand(connectionId, "password123");
            Thread.Sleep(50);
        }
    }

    public void Dispose()
    {
        _gameLoop.Stop();

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
    public void QueueCommand_EnqueuesCommand()
    {
        _gameLoop.QueueCommand("conn-1", "test hello");

        // Since we haven't started the game loop, commands should queue up
        // We can't directly inspect the queue, but we can test through processing
    }

    [Fact]
    public void CreatePlayerSession_CreatesSessionInLoginState()
    {
        _gameLoop.CreatePlayerSession("conn-1");

        var session = _gameLoop.GetSession("conn-1");

        Assert.NotNull(session);
        Assert.Equal("conn-1", session.ConnectionId);
        // Session starts in login state, no player object yet
        Assert.Null(session.PlayerObject);
        Assert.Equal(LoginState.AwaitingName, session.LoginState);
    }

    [Fact]
    public void RemovePlayerSession_RemovesSession()
    {
        _gameLoop.CreatePlayerSession("conn-1");
        Assert.NotNull(_gameLoop.GetSession("conn-1"));

        _gameLoop.RemovePlayerSession("conn-1");

        Assert.Null(_gameLoop.GetSession("conn-1"));
    }

    [Fact]
    public void GetAllSessions_ReturnsAllSessions()
    {
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.CreatePlayerSession("conn-2");
        _gameLoop.CreatePlayerSession("conn-3");

        var sessions = _gameLoop.GetAllSessions();

        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void SendToPlayer_QueuesOutput()
    {
        _gameLoop.SendToPlayer("conn-1", "Hello!");

        Assert.True(_gameLoop.TryDequeueOutput(out var output));
        Assert.Equal("conn-1", output!.ConnectionId);
        Assert.Equal("Hello!", output.Content);
    }

    [Fact]
    public void TryDequeueOutput_ReturnsFalseWhenEmpty()
    {
        Assert.False(_gameLoop.TryDequeueOutput(out var output));
        Assert.Null(output);
    }

    [Fact]
    public void StartAndStop_Works()
    {
        _gameLoop.Start();
        Thread.Sleep(50); // Give it time to start

        _gameLoop.Stop();
        // Should not throw
    }

    [Fact]
    public void ProcessesCommands_WhenRunning()
    {
        // Create an account for testing
        _accountManager.CreateAccount("testuser", "test@test.com", "password123");

        // Create session (starts in login state)
        _gameLoop.CreatePlayerSession("conn-1");

        // Start the game loop
        _gameLoop.Start();

        // Complete login flow
        _gameLoop.QueueCommand("conn-1", "testuser");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        // Now queue a game command
        _gameLoop.QueueCommand("conn-1", "test hello");

        // Wait for processing
        Thread.Sleep(300);

        // Stop the loop
        _gameLoop.Stop();

        // Check for output (command result + prompt)
        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        // Should have the test output
        Assert.Contains(outputs, o => o.Content.Contains("Test: hello"));
    }

    [Fact]
    public void SayCommand_WithNoArgs_ShowsError()
    {
        _accountManager.CreateAccount("saytest", "say@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Login first
        _gameLoop.QueueCommand("conn-1", "saytest");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        _gameLoop.QueueCommand("conn-1", "say");

        Thread.Sleep(300);
        _gameLoop.Stop();

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        Assert.Contains(outputs, o => o.Content.Contains("Say what?"));
    }

    [Fact]
    public void SayCommand_WithArgs_ShowsMessage()
    {
        _accountManager.CreateAccount("saytest2", "say2@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Login first
        _gameLoop.QueueCommand("conn-1", "saytest2");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        _gameLoop.QueueCommand("conn-1", "say hello world");

        Thread.Sleep(300);
        _gameLoop.Stop();

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        Assert.Contains(outputs, o => o.Content.Contains("You say: hello world"));
    }

    [Fact]
    public void UnknownCommand_ShowsError()
    {
        _accountManager.CreateAccount("unknowntest", "unknown@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Login first
        _gameLoop.QueueCommand("conn-1", "unknowntest");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        _gameLoop.QueueCommand("conn-1", "unknowncommand");

        Thread.Sleep(300);
        _gameLoop.Stop();

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        // Classic LPMud behavior: "What?" is the default failure message
        // Custom messages can be set via notify_fail()
        Assert.Contains(outputs, o => o.Content.Contains("What?"));
    }

    [Fact]
    public void EmptyCommand_SendsPrompt()
    {
        _accountManager.CreateAccount("emptytest", "empty@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Login first
        _gameLoop.QueueCommand("conn-1", "emptytest");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        _gameLoop.QueueCommand("conn-1", "");

        Thread.Sleep(300);
        _gameLoop.Stop();

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        Assert.Contains(outputs, o => o.Content.Contains(">"));
    }

    [Fact]
    public void CommandsProcessedForCorrectConnection()
    {
        _accountManager.CreateAccount("multi1", "multi1@test.com", "password123");
        _accountManager.CreateAccount("multi2", "multi2@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.CreatePlayerSession("conn-2");
        _gameLoop.Start();

        // Login both users
        _gameLoop.QueueCommand("conn-1", "multi1");
        _gameLoop.QueueCommand("conn-2", "multi2");
        Thread.Sleep(100);
        _gameLoop.QueueCommand("conn-1", "password123");
        _gameLoop.QueueCommand("conn-2", "password123");
        Thread.Sleep(200);

        _gameLoop.QueueCommand("conn-1", "test from-conn-1");
        _gameLoop.QueueCommand("conn-2", "test from-conn-2");

        Thread.Sleep(300);
        _gameLoop.Stop();

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        // Check that output went to correct connections
        var conn1Output = outputs.Where(o => o.ConnectionId == "conn-1").ToList();
        var conn2Output = outputs.Where(o => o.ConnectionId == "conn-2").ToList();

        Assert.Contains(conn1Output, o => o.Content.Contains("from-conn-1"));
        Assert.Contains(conn2Output, o => o.Content.Contains("from-conn-2"));
    }

    [Fact]
    public void Login_WithValidCredentials_EntersGame()
    {
        _accountManager.CreateAccount("logintest", "login@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Enter username
        _gameLoop.QueueCommand("conn-1", "logintest");
        Thread.Sleep(100);

        // Enter password
        _gameLoop.QueueCommand("conn-1", "password123");
        Thread.Sleep(200);

        _gameLoop.Stop();

        var session = _gameLoop.GetSession("conn-1");
        Assert.NotNull(session);
        Assert.Equal(LoginState.Playing, session.LoginState);
        Assert.NotNull(session.PlayerObject);
        Assert.Equal("logintest", session.AuthenticatedUsername);
    }

    [Fact]
    public void Login_WithInvalidPassword_ShowsError()
    {
        _accountManager.CreateAccount("badpasstest", "badpass@test.com", "password123");
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Enter username
        _gameLoop.QueueCommand("conn-1", "badpasstest");
        Thread.Sleep(100);

        // Enter wrong password
        _gameLoop.QueueCommand("conn-1", "wrongpassword");
        Thread.Sleep(200);

        _gameLoop.Stop();

        var session = _gameLoop.GetSession("conn-1");
        Assert.NotNull(session);
        // Should be back to awaiting name after failed login
        Assert.Equal(LoginState.AwaitingName, session.LoginState);
        Assert.Null(session.PlayerObject);

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        Assert.Contains(outputs, o => o.Content.Contains("Invalid password"));
    }

    [Fact]
    public void Registration_WithReservedName_ShowsError()
    {
        _gameLoop.CreatePlayerSession("conn-1");
        _gameLoop.Start();

        // Start registration
        _gameLoop.QueueCommand("conn-1", "new");
        Thread.Sleep(100);

        // Try to register as "new"
        _gameLoop.QueueCommand("conn-1", "new");
        Thread.Sleep(200);

        _gameLoop.Stop();

        var session = _gameLoop.GetSession("conn-1");
        Assert.NotNull(session);
        // Should still be in registration name state
        Assert.Equal(LoginState.RegistrationName, session.LoginState);

        var outputs = new List<OutputMessage>();
        while (_gameLoop.TryDequeueOutput(out var output))
        {
            outputs.Add(output!);
        }

        Assert.Contains(outputs, o => o.Content.Contains("reserved"));
    }
}
