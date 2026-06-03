namespace GenericsExercises;

using System.Numerics;

// Lesson 09 alignment: constraints, generic math (INumber<T>), reified generics.
public static class Solution
{
    // Sum any sequence of INumber<T>. Empty -> T.Zero.
    public static T Sum<T>(IEnumerable<T> xs) where T : INumber<T> =>
        throw new NotImplementedException();

    // Max by a key selector. Throw InvalidOperationException if source is empty.
    public static T MaxBy<T, TKey>(IEnumerable<T> source, Func<T, TKey> key)
        where TKey : IComparable<TKey>
        => throw new NotImplementedException();
}

// A tiny generic cache. The TEST will exercise it -- you implement.
// Lookup returns true + the cached value if present, otherwise calls `factory`,
// stores the result, and returns false + the new value.
public class Cache<TKey, TValue> where TKey : notnull
{
    public int Count => throw new NotImplementedException();

    public bool GetOrAdd(TKey key, Func<TKey, TValue> factory, out TValue value) =>
        throw new NotImplementedException();
}
