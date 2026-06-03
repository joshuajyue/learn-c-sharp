namespace AsyncStreamsExercises;

// Lesson 11 alignment: IAsyncEnumerable, Channels, batching.
public static class Solution
{
    // Yield batches of `size` items from the source. The final batch may be smaller
    // than `size` if the source doesn't divide evenly. Throw ArgumentOutOfRangeException
    // for size <= 0.
    public static async IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(
        IAsyncEnumerable<T> source, int size)
    {
        // TODO: implement. Tip: collect into a List<T>, yield + clear at every `size`.
        // Remember to yield the final partial batch (if any).
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        await Task.CompletedTask;
        throw new NotImplementedException();
#pragma warning disable CS0162
        yield break;     // makes this a valid iterator until you implement it
#pragma warning restore CS0162
    }

    // Helper used by the tests. Don't change.
    public static async IAsyncEnumerable<int> RangeAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
