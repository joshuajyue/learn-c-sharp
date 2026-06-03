namespace AsyncExercises;

// Lesson 10 alignment: async/await, cancellation, retry, fan-out.
public static class Solution
{
    // Run `action` up to `attempts` times. If it throws, wait `delay` (ignored on
    // last attempt) and retry. Honour the cancellation token throughout
    // (cancel during the delay should propagate OperationCanceledException).
    // Return the final successful T or rethrow the LAST exception.
    public static async Task<T> RetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int attempts,
        TimeSpan delay,
        CancellationToken cancel = default)
        => throw new NotImplementedException();

    // Start all tasks concurrently. Return the result of the FIRST one to
    // complete. (Use Task.WhenAny; you don't need to cancel the losers.)
    public static async Task<T> FirstAsync<T>(IEnumerable<Task<T>> tasks) =>
        throw new NotImplementedException();
}
