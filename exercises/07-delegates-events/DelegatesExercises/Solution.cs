namespace DelegatesExercises;

// Lesson 07 alignment: Func/Action composition, multicast, events.
public static class Solution
{
    // Apply each transform in order: result of step 1 becomes input to step 2, etc.
    // Pipeline([]) should return seed unchanged.
    public static T Pipeline<T>(T seed, params Func<T, T>[] steps) =>
        throw new NotImplementedException();
}

// A counter that raises an event for every increment. Tests subscribe and
// verify the handler is invoked the right number of times with the right values.
public class Counter
{
    public int Value { get; private set; }

    public event EventHandler<int>? Incremented;

    // Increment Value by `by`, then raise Incremented with the NEW value.
    public void Increment(int by = 1) => throw new NotImplementedException();
}
