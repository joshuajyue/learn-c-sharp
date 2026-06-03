namespace UnsafeExercises;

public class Tests
{
    [Fact]
    public void ReverseInPlace_basic()
    {
        var arr = new[] { 1, 2, 3, 4, 5 };
        Solution.ReverseInPlace(arr);
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, arr);
    }

    [Fact]
    public void ReverseInPlace_even_length()
    {
        var arr = new[] { 1, 2, 3, 4 };
        Solution.ReverseInPlace(arr);
        Assert.Equal(new[] { 4, 3, 2, 1 }, arr);
    }

    [Fact]
    public void ReverseInPlace_empty()
    {
        var arr = Array.Empty<int>();
        Solution.ReverseInPlace(arr);
        Assert.Empty(arr);
    }

    [Fact]
    public void SumWithPointer_works() =>
        Assert.Equal(150L, Solution.SumWithPointer(new[] { 10, 20, 30, 40, 50 }));

    [Fact]
    public void SumWithPointer_empty() =>
        Assert.Equal(0L, Solution.SumWithPointer(Array.Empty<int>()));

    [Fact]
    public void DoubleBits_matches_BitConverter()
    {
        Assert.Equal(BitConverter.DoubleToInt64Bits(1.5),  Solution.DoubleBits(1.5));
        Assert.Equal(BitConverter.DoubleToInt64Bits(0.0),  Solution.DoubleBits(0.0));
        Assert.Equal(BitConverter.DoubleToInt64Bits(-Math.PI), Solution.DoubleBits(-Math.PI));
    }
}
