namespace SourceGenExercises;

using Microsoft.Extensions.Logging;

public class Tests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = new();
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
                                Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((level, eventId, formatter(state, exception)));
    }

    [Fact]
    public void LoggerMessage_emits_expected_entry()
    {
        var log = new CapturingLogger();
        Solution.LogUserAction(log, userId: 7, action: "login");

        var e = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Information, e.Level);
        Assert.Equal(42, e.EventId.Id);
        Assert.Equal("User 7 did login", e.Message);
    }

    [Fact]
    public void GeneratedRegex_matches_and_captures()
    {
        var m = Solution.OrderIdRegex().Match("ABC-123");
        Assert.True(m.Success);
        Assert.Equal("ABC", m.Groups[1].Value);
        Assert.Equal("123", m.Groups[2].Value);
    }

    [Fact]
    public void GeneratedRegex_rejects_invalid()
    {
        Assert.False(Solution.OrderIdRegex().Match("abc-123").Success);
        Assert.False(Solution.OrderIdRegex().Match("ABCD-123").Success);
        Assert.False(Solution.OrderIdRegex().Match("ABC123").Success);
    }
}
