namespace GenericsExercises;

public class Tests
{
    [Fact]
    public void Sum_int_empty() => Assert.Equal(0, Solution.Sum(Array.Empty<int>()));

    [Fact]
    public void Sum_int_values() => Assert.Equal(10, Solution.Sum(new[] { 1, 2, 3, 4 }));

    [Fact]
    public void Sum_double_values() => Assert.Equal(7.0, Solution.Sum(new[] { 1.5, 2.5, 3.0 }));

    [Fact]
    public void MaxBy_returns_max_element()
    {
        var staff = new[] { ("Ada", 36), ("Linus", 54), ("Grace", 85) };
        var oldest = Solution.MaxBy(staff, p => p.Item2);
        Assert.Equal("Grace", oldest.Item1);
    }

    [Fact]
    public void MaxBy_empty_throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            Solution.MaxBy(Array.Empty<int>(), x => x));

    [Fact]
    public void Cache_GetOrAdd_first_call_invokes_factory()
    {
        var c = new Cache<string, int>();
        var hit = c.GetOrAdd("x", k => k.Length, out var v);
        Assert.False(hit);
        Assert.Equal(1, v);
        Assert.Equal(1, c.Count);
    }

    [Fact]
    public void Cache_GetOrAdd_second_call_uses_cache()
    {
        var c = new Cache<string, int>();
        int calls = 0;
        c.GetOrAdd("x", k => { calls++; return 42; }, out _);
        var hit = c.GetOrAdd("x", k => { calls++; return -1; }, out var v);
        Assert.True(hit);
        Assert.Equal(42, v);
        Assert.Equal(1, calls);
    }
}
