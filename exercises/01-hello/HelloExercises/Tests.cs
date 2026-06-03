namespace HelloExercises;

public class Tests
{
    [Theory]
    [InlineData("Ada", "Hello, Ada!")]
    [InlineData("", "Hello, stranger!")]
    [InlineData(null, "Hello, stranger!")]
    [InlineData("  ", "Hello, stranger!")]
    public void Greet_returns_expected(string? input, string expected) =>
        Assert.Equal(expected, Solution.Greet(input));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 15)]
    [InlineData(10, 55)]
    public void SumTo_works(int n, int expected) =>
        Assert.Equal(expected, Solution.SumTo(n));

    [Fact]
    public void SumTo_negative_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Solution.SumTo(-1));

    [Fact]
    public void FirstNonEmpty_picks_first_real_value() =>
        Assert.Equal("Ada", Solution.FirstNonEmptyOrDefault(null, "", "  ", "Ada", "Linus"));

    [Fact]
    public void FirstNonEmpty_falls_back_to_default() =>
        Assert.Equal("default", Solution.FirstNonEmptyOrDefault(null, "", "  "));
}
