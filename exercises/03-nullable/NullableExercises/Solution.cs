namespace NullableExercises;

// Lesson 03 alignment: nullable reference types, ?., ??, ??=, NRT annotations.
public static class Solution
{
    // Return the string's length, or null if the input is null.
    public static int? SafeLength(string? s) => throw new NotImplementedException();

    // Return the first non-null reference in args, or null if all are null.
    // Hint: pick a constraint on T that allows nulls -- `where T : class`.
    public static T? FirstNonNull<T>(params T?[] args) where T : class =>
        throw new NotImplementedException();

    // Return value.Trim().ToUpperInvariant() if non-null/non-empty/non-whitespace;
    // else return fallback. Must compile cleanly with no null-warning suppressions (`!`).
    public static string NormalizeOrFallback(string? value, string fallback) =>
        throw new NotImplementedException();
}
