// Lesson 15: Dependency injection, IOptions, IHostedService
//
// This is the mental model the entire `dotnet/extensions` repo is built around,
// and ASP.NET Core, Worker Services, EF Core, and most modern .NET apps consume.
//
// Three ideas to internalise:
//   1. SERVICE COLLECTION:  you describe "what implements what" up front.
//        services.AddSingleton<IClock, SystemClock>();
//        services.AddScoped<IRepo, SqlRepo>();
//        services.AddTransient<Foo>();
//   2. SERVICE PROVIDER:    built once from the collection; resolves graphs
//      via CONSTRUCTOR INJECTION. No reflection magic on YOUR side.
//   3. LIFETIMES:
//        Singleton  -- one per container, lives forever.
//        Scoped     -- one per "scope" (per HTTP request in ASP.NET).
//        Transient  -- one per call to GetService.
//
// `IOptions<T>` is how config is delivered to your services. `IHostedService`
// is the long-running background-task contract (think Java's @Scheduled or a
// daemon). `IHost` ties it all together: config + DI + logging + lifetime.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// --- Configuration: in-memory for the demo; in prod you'd load JSON/env/etc. ---
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Greeter:Prefix"] = "Hello",
    ["Greeter:Loud"]   = "true"
});

// --- Bind a config section to a strongly-typed options class ---
builder.Services.Configure<GreeterOptions>(builder.Configuration.GetSection("Greeter"));

// --- Register services with explicit lifetimes ---
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddTransient<IGreeter, Greeter>();
builder.Services.AddHostedService<DemoWorker>();   // runs as a background service

using var host = builder.Build();

// Resolve manually for the demo so we can show output before the host shuts down.
// (In a real app, DemoWorker.ExecuteAsync would be the entry point.)
using (var scope = host.Services.CreateScope())
{
    var greeter = scope.ServiceProvider.GetRequiredService<IGreeter>();
    Console.WriteLine(greeter.Greet("Ada"));
    Console.WriteLine(greeter.Greet("Linus"));
}

// Run the host briefly so the hosted service starts/stops.
var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
try { await host.RunAsync(cts.Token); }
catch (OperationCanceledException) { /* expected on timed shutdown */ }

// --- Types ---

// Options type: plain POCO with init/get-set, bound from configuration.
sealed class GreeterOptions
{
    public string Prefix { get; set; } = "Hi";
    public bool   Loud   { get; set; }
}

interface IClock { DateTime Now { get; } }
sealed class SystemClock : IClock { public DateTime Now => DateTime.Now; }

interface IGreeter { string Greet(string who); }

// Constructor injection. The container instantiates Greeter and supplies:
//   * IOptions<GreeterOptions>  -- snapshot of bound config
//   * IClock                    -- the singleton registered above
// Use IOptionsMonitor<T> instead of IOptions<T> if you need live updates when
// configuration reloads (e.g. file change).
sealed class Greeter(IOptions<GreeterOptions> opts, IClock clock) : IGreeter
{
    private readonly GreeterOptions _o = opts.Value;

    public string Greet(string who)
    {
        var msg = $"{_o.Prefix}, {who}! ({clock.Now:HH:mm:ss})";
        return _o.Loud ? msg.ToUpperInvariant() : msg;
    }
}

// IHostedService = "thing that starts when the host starts and stops when it
// stops". BackgroundService is the convenient base class — you only override
// ExecuteAsync. Used for queue consumers, schedulers, message brokers, etc.
sealed class DemoWorker(ILogger<DemoWorker> log, IGreeter greeter) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            log.LogInformation("{Greeting}", greeter.Greet("worker"));
            try { await Task.Delay(50, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        log.LogInformation("worker stopped");
    }
}
