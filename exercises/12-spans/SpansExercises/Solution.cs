namespace SpansExercises;

// Lesson 12 alignment: Span<T>, ReadOnlySpan<char>, allocation-free parsing.
//
// IMPORTANT: every method here MUST NOT allocate intermediate strings.
//   * No .Split(' ')  (returns string[])
//   * No .Substring() (returns new string)
//   * No string concatenation in the hot loop
// Tests don't measure allocations, but the spirit of the exercise is to use
// Span<char> slicing only.
public static class Solution
{
    // Sum a span of comma-separated ints. "10,20,30" -> 60. Empty span -> 0.
    public static int SumCsvInts(ReadOnlySpan<char> csv) =>
        throw new NotImplementedException();

    // Count whitespace-separated words. Treat any run of whitespace as one separator.
    // "  hello   world " -> 2. "" or whitespace-only -> 0.
    public static int CountWords(ReadOnlySpan<char> text) =>
        throw new NotImplementedException();

    // Reverse a span in place. (Demonstrates Span<T> mutability.)
    public static void ReverseInPlace<T>(Span<T> span) =>
        throw new NotImplementedException();
}
