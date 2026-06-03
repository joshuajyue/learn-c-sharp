namespace DiExercises;

using Microsoft.Extensions.DependencyInjection;

public class Tests
{
    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        Solution.Configure(services);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void Clock_singleton_returns_frozen_time()
    {
        using var sp = Build();
        var c1 = sp.GetRequiredService<IClock>();
        var c2 = sp.GetRequiredService<IClock>();
        Assert.Same(c1, c2);
        Assert.Equal(new DateTime(2025, 1, 1), c1.Now);
    }

    [Fact]
    public void Counter_singleton_shares_state_across_scopes()
    {
        using var sp = Build();
        using (var s1 = sp.CreateScope()) Assert.Equal(1, s1.ServiceProvider.GetRequiredService<ICounter>().Next());
        using (var s2 = sp.CreateScope()) Assert.Equal(2, s2.ServiceProvider.GetRequiredService<ICounter>().Next());
    }

    [Fact]
    public void RequestId_scoped_same_within_scope_different_across()
    {
        using var sp = Build();
        Guid a1, a2, b1;
        using (var s = sp.CreateScope())
        {
            a1 = s.ServiceProvider.GetRequiredService<IRequestId>().Id;
            a2 = s.ServiceProvider.GetRequiredService<IRequestId>().Id;
        }
        using (var s = sp.CreateScope())
        {
            b1 = s.ServiceProvider.GetRequiredService<IRequestId>().Id;
        }
        Assert.Equal(a1, a2);
        Assert.NotEqual(a1, b1);
    }
}
