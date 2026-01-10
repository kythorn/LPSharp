using System.Collections.Concurrent;
using Xunit;

namespace Driver.Tests;

public class ExecutionContextTests
{
    [Fact]
    public void Current_IsNullByDefault()
    {
        // Ensure we start fresh
        ExecutionContext.Current = null;

        Assert.Null(ExecutionContext.Current);
    }

    [Fact]
    public void Current_CanBeSetAndRead()
    {
        var context = new ExecutionContext
        {
            ConnectionId = "test-123"
        };

        ExecutionContext.Current = context;

        Assert.Same(context, ExecutionContext.Current);
        Assert.Equal("test-123", ExecutionContext.Current.ConnectionId);

        // Cleanup
        ExecutionContext.Current = null;
    }

    [Fact]
    public void Execute_SetsCurrentContext()
    {
        ExecutionContext? insideContext = null;
        var context = new ExecutionContext
        {
            ConnectionId = "test-exec"
        };

        context.Execute(() =>
        {
            insideContext = ExecutionContext.Current;
        });

        Assert.Same(context, insideContext);
        // After Execute, context should be restored to null
        Assert.Null(ExecutionContext.Current);
    }

    [Fact]
    public void Execute_RestoresPreviousContext()
    {
        var outer = new ExecutionContext { ConnectionId = "outer" };
        var inner = new ExecutionContext { ConnectionId = "inner" };

        ExecutionContext.Current = outer;

        inner.Execute(() =>
        {
            Assert.Same(inner, ExecutionContext.Current);
        });

        Assert.Same(outer, ExecutionContext.Current);

        // Cleanup
        ExecutionContext.Current = null;
    }

    [Fact]
    public void Execute_WithReturnValue()
    {
        var context = new ExecutionContext
        {
            ConnectionId = "test-return"
        };

        var result = context.Execute(() =>
        {
            return 42L;
        });

        Assert.Equal(42L, result);
    }

    [Fact]
    public void SendOutput_QueuesMessage()
    {
        var outputQueue = new ConcurrentQueue<OutputMessage>();
        var context = new ExecutionContext
        {
            ConnectionId = "test-send",
            OutputQueue = outputQueue
        };

        context.SendOutput("Hello, world!");

        Assert.True(outputQueue.TryDequeue(out var message));
        Assert.Equal("test-send", message.ConnectionId);
        Assert.Equal("Hello, world!", message.Content);
    }

    [Fact]
    public void SendOutput_DoesNothingWithNullQueue()
    {
        var context = new ExecutionContext
        {
            ConnectionId = "test-null-queue",
            OutputQueue = null
        };

        // Should not throw
        context.SendOutput("Hello");
    }

    [Fact]
    public void SendOutput_DoesNothingWithEmptyConnectionId()
    {
        var outputQueue = new ConcurrentQueue<OutputMessage>();
        var context = new ExecutionContext
        {
            ConnectionId = "",
            OutputQueue = outputQueue
        };

        context.SendOutput("Hello");

        Assert.True(outputQueue.IsEmpty);
    }
}

public class PlayerCommandTests
{
    [Fact]
    public void PlayerCommand_HasCorrectDefaults()
    {
        var cmd = new PlayerCommand();

        Assert.Equal(string.Empty, cmd.ConnectionId);
        Assert.Equal(string.Empty, cmd.Input);
        Assert.True((DateTime.UtcNow - cmd.Timestamp).TotalSeconds < 1);
    }

    [Fact]
    public void PlayerCommand_CanBeInitialized()
    {
        var timestamp = DateTime.UtcNow;
        var cmd = new PlayerCommand
        {
            ConnectionId = "conn-1",
            Input = "say hello",
            Timestamp = timestamp
        };

        Assert.Equal("conn-1", cmd.ConnectionId);
        Assert.Equal("say hello", cmd.Input);
        Assert.Equal(timestamp, cmd.Timestamp);
    }
}

public class OutputMessageTests
{
    [Fact]
    public void OutputMessage_HasCorrectDefaults()
    {
        var msg = new OutputMessage();

        Assert.Equal(string.Empty, msg.ConnectionId);
        Assert.Equal(string.Empty, msg.Content);
    }

    [Fact]
    public void OutputMessage_CanBeInitialized()
    {
        var msg = new OutputMessage
        {
            ConnectionId = "conn-1",
            Content = "Hello, player!"
        };

        Assert.Equal("conn-1", msg.ConnectionId);
        Assert.Equal("Hello, player!", msg.Content);
    }
}

public class PlayerSessionTests
{
    [Fact]
    public void PlayerSession_HasCorrectDefaults()
    {
        var session = new PlayerSession();

        Assert.Equal(string.Empty, session.ConnectionId);
        Assert.Null(session.PlayerObject);
        Assert.True((DateTime.UtcNow - session.CreatedAt).TotalSeconds < 1);
        Assert.True((DateTime.UtcNow - session.LastActivity).TotalSeconds < 1);
    }

    [Fact]
    public void PlayerSession_LastActivity_CanBeUpdated()
    {
        var session = new PlayerSession
        {
            ConnectionId = "conn-1"
        };

        var newTime = DateTime.UtcNow.AddMinutes(5);
        session.LastActivity = newTime;

        Assert.Equal(newTime, session.LastActivity);
    }
}
