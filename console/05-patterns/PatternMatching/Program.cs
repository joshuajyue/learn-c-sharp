// Lesson 05: Pattern matching & switch expressions
//
// Java got pattern matching for `instanceof` and `switch` recently; C# has had a
// richer flavour for years. The point: deconstruct + test + bind in one expression.

object[] things = [42, "hello", 3.14, new Point(1, 2), new Point(0, 0), new Point(-1, -1), null!, new[] { 1, 2, 3 }];
// J: What does null! mean?
// C: The `!` is the null-forgiving operator.
// It tells the compiler that we are intentionally assigning `null` to a variable that is not nullable, and that we will handle it safely.
// C: If we didn't put the `!`, the compiler would give us a warning because `null` is not a valid value for a non-nullable reference type.
foreach (var x in things)
{
    Console.WriteLine($"{Describe(x),-40}  ({x ?? "<null>"})");
}
// J: What does -40 do in the above line?
// C: It left-aligns the string in a field of width 40.

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
    Point (0, 0)                          => "origin",                     // positional, which is a property pattern that matches the deconstruct method
    Point { X:  < 0, Y: < 0 }             => "negative point",             // property pattern with relational patterns
    Point { X: var px, Y: var py }        => $"point ({px},{py})",         // property, captures them
    int[] arr when arr is [1, 2, ..]      => "int array starting 1, 2",    // list pattern
    Array a                               => $"array of length {a.Length}",
    _                                     => "something else"
};
// J: Some of these make sense but I'm a bit confused by the syntax. For example, what does `int n when n < 0` mean?
// C: This is a pattern that matches an `int` and binds it to the variable `n`, but only if `n < 0` is true.
// J: Let's take the entry 42, which is a boxed int. How does C# know to match it to the `int n` pattern?
// C: The `switch` expression will try to match the patterns in order. It will first check if `x` is `null`, then if it is `0`, then if it is an `int`
// J: But its boxed, right? I thought the computer would see it as an `object` and not an `int`.
// C: In C#, when you have a boxed value type (like `int`), the pattern matching will unbox it if the pattern is an `int`.
// J: Can I get a parallel to Java?
// C: In Java, you would use `instanceof` to check if an object is of a certain type and then cast it.
// J: So it speedruns all that in one expression?
// C: Yes, the pattern matching in C# allows you to check the type and cast it in one step.
// J: Is it possible to do switch statements like this in Java? I've only ever done switch statements like `switch (x) { case 1: ... }`
// C: Java has recently added pattern matching for `instanceof` and `switch`, but it's not as powerful as C#'s pattern matching.

// J: So all of this is a wrapper around the expression x is type y, which returns a boolean and then stores the casted value in y?
// C: Yes, but its much more powerful than that. We can match against constants, properties, records, logical patterns, lists, do a property capture.


// J: I feel like the syntax is a bit weird here. Why is the switch outside of the method body?
// C: The `switch` is an expression, not a statement
// This is a little bit different from Java: in C#, you can use => to define a method that consists of a single expression
// For example, static double Pi() => 3.14; defines a method that returns 3.14
// In this case, the syntax is switch{}.


// `is` pattern can also be used in plain control flow and even as a guard inside `if`:
object o = "world";
if (o is string s && s.StartsWith("w"))
{
    Console.WriteLine($"starts with w: {s}");
}

record Point(int X, int Y);

// Do i have this right? records are reference types, but they satisfy value equality.
// Basically, its a class that has an automatic Equals and GetHashCodemethod.

