// Lesson 05: Pattern matching & switch expressions
//
// Java got pattern matching for `instanceof` and `switch` recently; C# has had a
// richer flavour for years. The point: deconstruct + test + bind in one expression.

object[] things = [42, "hello", 3.14, new Point(1, 2), new Point(0, 0), null!, new[] { 1, 2, 3 }];

foreach (var x in things)
{
    Console.WriteLine($"{Describe(x),-40}  ({x ?? "<null>"})");
}

// `switch expression`: returns a value, exhaustive, no fall-through, no `break`.
// Patterns used below: type, declaration, property, positional, relational, logical,
// list, constant, `_` discard.
static string Describe(object? x) => x switch
{
    null                                  => "null reference",
    0                                     => "zero (int)",                 // constant
    int n when n < 0                      => "negative int",
    int n                                 => $"positive int {n}",          // declaration
    string { Length: 0 }                  => "empty string",               // property
    string s                              => $"string of length {s.Length}",
    double d and (< 0 or > 100)           => $"out-of-range double {d}",   // logical
    double d                              => $"in-range double {d}",
    Point (0, 0)                          => "origin",                     // positional
    Point { X: var px, Y: var py }        => $"point ({px},{py})",         // property
    int[] arr when arr is [1, 2, ..]      => "int array starting 1, 2",    // list pattern
    Array a                               => $"array of length {a.Length}",
    _                                     => "something else"
};

// `is` pattern can also be used in plain control flow and even as a guard inside `if`:
object o = "world";
if (o is string s && s.StartsWith("w"))
{
    Console.WriteLine($"starts with w: {s}");
}

record Point(int X, int Y);
