namespace ReflectionExercises;

public class Tests
{
    public class Book
    {
        [Column("id")]    public int    Id    { get; set; }
        [Column("title")] public string Title { get; set; } = "";
        public string? Author { get; set; }       // no [Column] -> use property name
    }

    [Fact]
    public void Serialize_uses_column_names_and_property_names()
    {
        var b = new Book { Id = 7, Title = "Refactoring", Author = "Fowler" };
        Assert.Equal("id=7;title=Refactoring;Author=Fowler", Solution.Serialize(b));
    }

    [Fact]
    public void Serialize_null_property_becomes_empty()
    {
        var b = new Book { Id = 1, Title = "X", Author = null };
        Assert.Equal("id=1;title=X;Author=", Solution.Serialize(b));
    }
}
