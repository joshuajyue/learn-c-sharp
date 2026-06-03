namespace SpansExercises;

public class Tests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("42", 42)]
    [InlineData("10,20,30", 60)]
    [InlineData("1,2,3,4,5,6,7,8,9,10", 55)]
    public void SumCsvInts(string csv, int expected) =>
        Assert.Equal(expected, Solution.SumCsvInts(csv.AsSpan()));

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("hello", 1)]
    [InlineData("hello world", 2)]
    [InlineData("  hello   world ", 2)]
    [InlineData("a b c d e", 5)]
    public void CountWords(string text, int expected) =>
        Assert.Equal(expected, Solution.CountWords(text.AsSpan()));

    [Fact]
    public void ReverseInPlace_ints()
    {
        Span<int> s = stackalloc[] { 1, 2, 3, 4, 5 };
        Solution.ReverseInPlace(s);
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, s.ToArray());
    }

    [Fact]
    public void ReverseInPlace_strings()
    {
        var arr = new[] { "a", "b", "c" };
        Solution.ReverseInPlace(arr.AsSpan());
        Assert.Equal(new[] { "c", "b", "a" }, arr);
    }
}
