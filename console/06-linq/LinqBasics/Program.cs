// Lesson 06: LINQ basics
//
// LINQ ≈ Java Streams. Two flavours:
//   1. Method syntax (extension methods on IEnumerable<T>): .Where(...).Select(...)
//   2. Query syntax (SQL-ish keywords): from x in xs where ... select ...
// Both compile to the same thing; query syntax is just sugar over methods.
//
// Key behaviours to remember:
//   * Most operators are DEFERRED. They don't execute until you iterate
//     (foreach, ToList, ToArray, Count, etc.). Same as Java Streams.
//   * `IEnumerable<T>` is the lazy stream; `List<T>` is the eager collection.

var people = new[]
{
    new Person("Ada",   36, "math"),
    new Person("Linus", 54, "kernel"),
    new Person("Grace", 85, "math"),
    new Person("Bjarne",73, "lang"),
    new Person("Anders",62, "lang"),
};

// --- Method syntax ---
var youngMathFolks = people
    .Where(p => p.Field == "math")
    .OrderBy(p => p.Age)
    .Select(p => $"{p.Name} ({p.Age})")
    .ToList();          // ToList materializes -- now it's a real List<string>.

Console.WriteLine("math folks by age: " + string.Join(", ", youngMathFolks));

// --- Query syntax (equivalent) ---
var langFolks =
    from p in people
    where p.Field == "lang"
    orderby p.Name
    select $"{p.Name} ({p.Age})";

Console.WriteLine("lang folks: " + string.Join(", ", langFolks));

// --- Aggregations ---
Console.WriteLine($"count={people.Count()}  avgAge={people.Average(p => p.Age):F1}  oldest={people.Max(p => p.Age)}");
// what does F1 do? floating point with one digit
// --- Grouping ---
foreach (var g in people.GroupBy(p => p.Field).OrderBy(g => g.Key))
{
    Console.WriteLine($"  {g.Key}: {string.Join(", ", g.Select(p => p.Name))}");
}

// --- Deferred execution gotcha ---
// `query` is re-evaluated every time you iterate. Mutate the source and the query
// reflects it. If you don't want that, call .ToList() once and reuse the list.
var query = people.Where(p => p.Age > 60).Select(p => p.Name);
Console.WriteLine("over 60 (1st): " + string.Join(", ", query));
// (If we appended to `people` here, the next iteration would see the new entries.)

record Person(string Name, int Age, string Field);

// Lesson summary: streams, but you can do SQL language. A small explanation of deferred execution is that the query will only run upon iteration
// For example, if we created a query over a list, added an element to the list, and then iterated over the query, the new element would be included