namespace DelegatesExercises;

public class Tests
{
    [Fact]
    public void Pipeline_empty_returns_seed() =>
        Assert.Equal(5, Solution.Pipeline(5));

    [Fact]
    public void Pipeline_chains_in_order()
    {
        int result = Solution.Pipeline(2,
            x => x + 3,    // 5
            x => x * 4,    // 20
            x => x - 1);   // 19
        Assert.Equal(19, result);
    }

    [Fact]
    public void Pipeline_works_on_strings()
    {
        string result = Solution.Pipeline("hi",
            s => s + "!",
            s => s.ToUpperInvariant());
        Assert.Equal("HI!", result);
    }

    [Fact]
    public void Counter_raises_event_with_new_value()
    {
        var c = new Counter();
        var observed = new List<int>();
        c.Incremented += (_, v) => observed.Add(v);

        c.Increment();
        c.Increment(5);
        c.Increment();

        Assert.Equal(7, c.Value);
        Assert.Equal(new[] { 1, 6, 7 }, observed);
    }

    [Fact]
    public void Counter_no_subscribers_does_not_throw()
    {
        var c = new Counter();
        c.Increment();   // should not NRE on null event field
        Assert.Equal(1, c.Value);
    }
}
