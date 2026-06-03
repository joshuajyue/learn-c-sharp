namespace DisposableExercises;

public class Tests
{
    [Fact]
    public void Nested_using_pops_in_reverse()
    {
        var t = new ScopeTracker();
        using (t.Begin("outer"))
        {
            Assert.Equal(new[] { "outer" }, t.Open);
            using (t.Begin("inner"))
            {
                Assert.Equal(new[] { "inner", "outer" }, t.Open);
            }
            Assert.Equal(new[] { "outer" }, t.Open);
        }
        Assert.Empty(t.Open);
    }

    [Fact]
    public void Dispose_runs_on_exception()
    {
        var t = new ScopeTracker();
        Action act = () =>
        {
            using var s = t.Begin("a");
            throw new InvalidOperationException("boom");
        };
        Assert.Throws<InvalidOperationException>(act);
        Assert.Empty(t.Open);   // proves Dispose ran
    }

    [Fact]
    public void Double_dispose_is_idempotent()
    {
        var t = new ScopeTracker();
        var s = t.Begin("a");
        s.Dispose();
        s.Dispose();            // must not throw, must not pop again
        Assert.Empty(t.Open);
    }
}
