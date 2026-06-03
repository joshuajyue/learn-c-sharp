namespace AsyncExercises;

public class Tests
{
    [Fact]
    public async Task Retry_succeeds_first_try()
    {
        int calls = 0;
        var v = await Solution.RetryAsync<int>(_ =>
        {
            calls++;
            return Task.FromResult(42);
        }, attempts: 3, delay: TimeSpan.Zero);
        Assert.Equal(42, v);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Retry_succeeds_after_failures()
    {
        int calls = 0;
        var v = await Solution.RetryAsync<int>(_ =>
        {
            calls++;
            if (calls < 3) throw new InvalidOperationException("transient");
            return Task.FromResult(7);
        }, attempts: 5, delay: TimeSpan.Zero);
        Assert.Equal(7, v);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Retry_rethrows_after_exhausting_attempts()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Solution.RetryAsync<int>(_ => throw new InvalidOperationException("always"),
                                     attempts: 3, delay: TimeSpan.Zero));
    }

    [Fact]
    public async Task Retry_honours_cancellation_during_delay()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Solution.RetryAsync<int>(_ => throw new Exception("boom"),
                                     attempts: 10, delay: TimeSpan.FromSeconds(10),
                                     cancel: cts.Token));
    }

    [Fact]
    public async Task First_returns_winner()
    {
        var slow = Task.Run(async () => { await Task.Delay(200); return "slow"; });
        var fast = Task.Run(async () => { await Task.Delay(10);  return "fast"; });
        var result = await Solution.FirstAsync(new[] { slow, fast });
        Assert.Equal("fast", result);
    }
}
