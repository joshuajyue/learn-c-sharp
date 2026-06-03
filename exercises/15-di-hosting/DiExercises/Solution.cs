namespace DiExercises;

using Microsoft.Extensions.DependencyInjection;

// Lesson 15 alignment: register services with correct lifetimes so tests pass.
public interface IClock { DateTime Now { get; } }
public sealed class FrozenClock(DateTime at) : IClock { public DateTime Now { get; } = at; }

public interface ICounter { int Next(); }
public sealed class Counter : ICounter
{
    private int _n;
    public int Next() => ++_n;
}

public interface IRequestId { Guid Id { get; } }
public sealed class RequestId : IRequestId { public Guid Id { get; } = Guid.NewGuid(); }

public static class Solution
{
    // Configure the container so that:
    //   * IClock        -> SINGLETON instance returning DateTime(2025,1,1).
    //   * ICounter      -> SINGLETON Counter (every Next() across scopes increments same field).
    //   * IRequestId    -> SCOPED RequestId   (one per scope, same instance within a scope).
    public static void Configure(IServiceCollection services) =>
        throw new NotImplementedException();
}
