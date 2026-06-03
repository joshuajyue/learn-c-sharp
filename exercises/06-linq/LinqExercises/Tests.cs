namespace LinqExercises;

public class Tests
{
    private static readonly Employee[] Staff =
    [
        new("Ada",    "eng",   120m),
        new("Linus",  "eng",   180m),
        new("Grace",  "eng",   180m),
        new("Anders", "lang",  200m),
        new("Bjarne", "lang",  150m),
    ];

    [Fact]
    public void TotalPayroll() =>
        Assert.Equal(830m, Solution.TotalPayroll(Staff));

    [Fact]
    public void TopByDept_picks_highest()
    {
        var top = Solution.TopByDept(Staff);
        Assert.Equal(180m, top["eng"].Salary);
        Assert.Equal("Anders", top["lang"].Name);
    }

    [Fact]
    public void NamesSortedBySalaryDesc_then_name()
    {
        var names = Solution.NamesSortedBySalaryDesc(Staff);
        Assert.Equal(new[] { "Anders", "Grace", "Linus", "Bjarne", "Ada" }, names);
    }

    [Fact]
    public void AverageByDept()
    {
        var avg = Solution.AverageByDept(Staff);
        Assert.Equal(160m, avg["eng"]);
        Assert.Equal(175m, avg["lang"]);
    }
}
