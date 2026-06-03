namespace AsyncStreamsExercises;

public class Tests
{
    [Fact]
    public async Task BatchAsync_groups_into_full_batches()
    {
        var batches = new List<IReadOnlyList<int>>();
        await foreach (var b in Solution.BatchAsync(Solution.RangeAsync(6), 2))
            batches.Add(b);
        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 0, 1 }, batches[0]);
        Assert.Equal(new[] { 2, 3 }, batches[1]);
        Assert.Equal(new[] { 4, 5 }, batches[2]);
    }

    [Fact]
    public async Task BatchAsync_yields_partial_final_batch()
    {
        var batches = new List<IReadOnlyList<int>>();
        await foreach (var b in Solution.BatchAsync(Solution.RangeAsync(5), 2))
            batches.Add(b);
        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 4 }, batches[2]);
    }

    [Fact]
    public async Task BatchAsync_empty_source_yields_nothing()
    {
        var batches = new List<IReadOnlyList<int>>();
        await foreach (var b in Solution.BatchAsync(Solution.RangeAsync(0), 4))
            batches.Add(b);
        Assert.Empty(batches);
    }

    [Fact]
    public async Task BatchAsync_invalid_size_throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var _ in Solution.BatchAsync(Solution.RangeAsync(3), 0)) { }
        });
    }
}
