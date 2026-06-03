namespace DisposableExercises;

// Lesson 08 alignment: IDisposable, deterministic cleanup, exception safety.

// Tracks open scopes by pushing onto an internal stack. Dispose pops itself.
// On exception inside a `using` block the Dispose must still run -- that's the
// whole point of IDisposable, and the test verifies it.
public class ScopeTracker
{
    private readonly Stack<string> _stack = new();

    public IReadOnlyCollection<string> Open => _stack;

    // Open a new scope with the given name. The returned IDisposable must:
    //   * Push `name` onto _stack on creation.
    //   * Pop the TOP of _stack on Dispose (verify top equals name, else throw).
    //   * Be idempotent (a second Dispose is a no-op).
    public IDisposable Begin(string name) => throw new NotImplementedException();
}
