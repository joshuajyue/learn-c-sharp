using System.Reflection;

// Lesson 14: Attributes and reflection
//
// Attributes ≈ Java annotations: declarative metadata you can attach to almost
// any code element. They become entries in the assembly's metadata table; you
// read them at runtime with reflection or at compile time from a source
// generator / Roslyn analyzer.
//
// Reflection ≈ java.lang.reflect: walk types/members/attributes, invoke methods,
// create instances. Heavily used by serializers, DI containers, ORMs, test
// runners. It is also SLOW relative to compiled code — modern .NET libraries
// increasingly replace it with source generators (System.Text.Json,
// LoggerMessage, RegexGenerator, etc.).

// --- 1. Apply an attribute to a class and its members ---
var bookType = typeof(Book);

// --- 2. Read attributes via reflection ---
var entity = bookType.GetCustomAttribute<TableAttribute>();
Console.WriteLine($"[Book] -> table = {entity?.Name}");

foreach (var prop in bookType.GetProperties())
{
    var col = prop.GetCustomAttribute<ColumnAttribute>();
    Console.WriteLine($"  {prop.Name,-10} -> column={col?.Name ?? prop.Name}  type={prop.PropertyType.Name}");
}

// --- 3. Create an instance and set properties dynamically ---
// This is the bones of what a JSON deserializer does:
var bookObj = Activator.CreateInstance(bookType)!;
bookType.GetProperty(nameof(Book.Id))!.SetValue(bookObj, 7);
bookType.GetProperty(nameof(Book.Title))!.SetValue(bookObj, "Refactoring");
Console.WriteLine($"constructed via reflection: {bookObj}");

// --- 4. Invoke a method by name ---
var method = bookType.GetMethod(nameof(Book.Summarize))!;
var summary = method.Invoke(bookObj, parameters: null);
Console.WriteLine($"invoked: {summary}");

// --- 5. AttributeUsage controls where an attribute can be applied ---
// (See ColumnAttribute below: Property only, not inherited.)

[Table("books")]
class Book
{
    [Column("id")]    public int    Id    { get; set; }
    [Column("title")] public string Title { get; set; } = "";

    public string Summarize() => $"Book #{Id}: {Title}";
    public override string ToString() => Summarize();
}

// Custom attributes are just classes inheriting from Attribute.
// Conventional suffix: "Attribute". Usage drops the suffix: [Table("...")].
[AttributeUsage(AttributeTargets.Class)]
sealed class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
sealed class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
