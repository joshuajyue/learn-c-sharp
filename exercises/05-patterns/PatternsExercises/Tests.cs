namespace PatternsExercises;

public class Tests
{
    [Fact]
    public void Area_circle() =>
        Assert.Equal(Math.PI * 4, Solution.Area(new Circle(2)), precision: 9);

    [Fact]
    public void Area_rectangle() =>
        Assert.Equal(12.0, Solution.Area(new Rectangle(3, 4)));

    [Fact]
    public void Area_triangle() =>
        Assert.Equal(6.0, Solution.Area(new Triangle(3, 4)));

    [Theory]
    [InlineData(null, "null")]
    [InlineData(0, "zero")]
    [InlineData(-3, "negative int")]
    [InlineData(7, "positive int")]
    [InlineData("", "empty string")]
    [InlineData("abc", "string")]
    [InlineData(1.5, "other")]
    public void Classify_works(object? input, string expected) =>
        Assert.Equal(expected, Solution.Classify(input));

    [Fact]
    public void Classify_list_pattern() =>
        Assert.Equal("starts 1,2", Solution.Classify(new[] { 1, 2, 3, 4 }));
}
