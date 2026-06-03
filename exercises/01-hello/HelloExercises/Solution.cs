namespace HelloExercises;

// Lesson 01 alignment: top-level statements, var, string interpolation, basic args.
//
// Implement each method. Each one starts as `throw new NotImplementedException()`.
// Run `dotnet test` in this folder to see failing tests turn green.
public static class Solution
{
    // Return "Hello, {name}!". If name is null/empty/whitespace, use "stranger".
    public static string Greet(string? name) => throw new NotImplementedException();

    // Sum of 1..n inclusive. SumTo(0) == 0. SumTo(5) == 15.
    // Throw ArgumentOutOfRangeException for n < 0.
    public static int SumTo(int n) => throw new NotImplementedException();

    // Pick the first non-null/non-empty/non-whitespace argument, or "default" if none.
    public static string FirstNonEmptyOrDefault(params string?[] args) =>
        throw new NotImplementedException();
}
