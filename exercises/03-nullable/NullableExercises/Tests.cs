namespace NullableExercises;

public class Tests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    public void SafeLength_handles_null(string? input, int? expected) =>
        Assert.Equal(expected, Solution.SafeLength(input));

    [Fact]
    public void FirstNonNull_picks_first() =>
        Assert.Equal("hit", Solution.FirstNonNull<string>(null, null, "hit", "miss"));

    [Fact]
    public void FirstNonNull_returns_null_when_all_null() =>
        Assert.Null(Solution.FirstNonNull<string>(null, null, null));

    [Theory]
    [InlineData("  ada  ", "fallback", "ADA")]
    [InlineData("", "fallback", "fallback")]
    [InlineData(null, "fallback", "fallback")]
    [InlineData("   ", "fallback", "fallback")]
    public void NormalizeOrFallback_works(string? v, string fb, string expected) =>
        Assert.Equal(expected, Solution.NormalizeOrFallback(v, fb));
}
