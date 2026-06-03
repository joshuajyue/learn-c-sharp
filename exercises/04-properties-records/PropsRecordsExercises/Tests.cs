namespace PropsRecordsExercises;

public class Tests
{
    [Fact]
    public void MakeMoney_constructs()
    {
        var m = Solution.MakeMoney(10m, "USD");
        Assert.Equal(10m, m.Amount);
        Assert.Equal("USD", m.Currency);
    }

    [Fact]
    public void Records_compare_by_value()
    {
        Assert.Equal(new Money(5, "EUR"), new Money(5, "EUR"));
        Assert.NotEqual(new Money(5, "EUR"), new Money(5, "USD"));
    }

    [Fact]
    public void Convert_uses_with_expression()
    {
        var m = new Money(10m, "USD");
        var c = Solution.Convert(m, "EUR");
        Assert.Equal(new Money(10m, "EUR"), c);
        Assert.Equal("USD", m.Currency);   // original unchanged
    }

    [Fact]
    public void Add_same_currency() =>
        Assert.Equal(new Money(7m, "USD"), Solution.Add(new Money(3m, "USD"), new Money(4m, "USD")));

    [Fact]
    public void Add_different_currency_throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            Solution.Add(new Money(1m, "USD"), new Money(1m, "EUR")));
}
