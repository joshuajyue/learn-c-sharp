namespace ValueRefExercises;

public class Tests
{
    [Fact]
    public void Swap_ints()
    {
        int a = 1, b = 2;
        Solution.Swap(ref a, ref b);
        Assert.Equal(2, a);
        Assert.Equal(1, b);
    }

    [Fact]
    public void Swap_strings()
    {
        string x = "hello", y = "world";
        Solution.Swap(ref x, ref y);
        Assert.Equal("world", x);
        Assert.Equal("hello", y);
    }

    [Fact]
    public void Translate_mutates_caller_struct()
    {
        var p = new PointStruct(1, 1);
        Solution.Translate(ref p, 4, 5);
        Assert.Equal(5, p.X);
        Assert.Equal(6, p.Y);
    }

    [Fact]
    public void Moved_returns_new_value_without_touching_original()
    {
        var p = new PointStruct(1, 1);
        var q = Solution.Moved(p, 4, 5);
        Assert.Equal(1, p.X);
        Assert.Equal(1, p.Y);
        Assert.Equal(5, q.X);
        Assert.Equal(6, q.Y);
    }
}
