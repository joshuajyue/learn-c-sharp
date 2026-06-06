// Lesson 04: Dependency injection in ASP.NET Core
//
// Recap from console lesson 15: dependency injection is "instead of `new`ing
// up your dependencies inside a class, the framework hands them to you via
// the constructor (or, in minimal APIs, via the handler's parameters)."
// You DECLARE what you need; the DI container builds it.
//
// In a web app the container has one extra trick: it can scope objects to a
// single HTTP request. That's important because:
//   * Many requests run AT THE SAME TIME on different threads (Kestrel uses
//     the thread pool). A SHARED mutable object would need locking everywhere.
//   * Most web work is "do something on behalf of this one request, then
//     forget about it" -- so per-request lifetimes are natural.
//
// Three lifetimes:
//   Singleton  -- ONE instance for the whole running app
//   Scoped     -- ONE instance per HTTP request (framework creates the scope
//                 when a request arrives and disposes it when the response is sent)
//   Transient  -- a NEW instance every time anything asks for it
//
// In a minimal API handler, any parameter whose TYPE is registered in the DI
// container is auto-injected. (Primitives like `int name` come from the route
// or query string instead -- see lesson 02.)
//
// Try:
//   curl http://localhost:5xxx/now
//   curl http://localhost:5xxx/visit    (call several times; watch the counts)

var builder = WebApplication.CreateBuilder(args);

// --- Register your own services ---
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<RequestCounter>();        // fresh per HTTP request
builder.Services.AddSingleton<TotalCounter>();       // one for the whole app

var app = builder.Build();

// IClock is resolved from the singleton registration. Every request gets the
// same SystemClock instance.
app.MapGet("/now", (IClock clock) => new { now = clock.Now });

// Two services injected at once:
//   * `req`   -- scoped, NEW instance per request -> Count is always 1 here
//   * `total` -- singleton, shared across requests -> Count grows each call
app.MapGet("/visit", (RequestCounter req, TotalCounter total) =>
{
    req.Bump();
    total.Bump();
    return new
    {
        thisRequest = req.Count,
        sinceStartup = total.Count,
    };
});

app.Run();

// --- Services ---
public interface IClock { DateTime Now { get; } }
public sealed class SystemClock : IClock { public DateTime Now => DateTime.UtcNow; }

// Scoped: lifetime = one HTTP request. Internal state is wiped when the
// request ends, so we can store mutable fields without worrying about
// other requests stomping on us.
public sealed class RequestCounter
{
    public int Count { get; private set; }
    public void Bump() => Count++;
}

// Singleton: lives as long as the app does. Because many requests on many
// threads can call Bump() simultaneously, use Interlocked.Increment to update
// the field atomically (a plain `_count++` would lose increments under load).
public sealed class TotalCounter
{
    private int _count;
    public int Count => _count;
    public void Bump() => Interlocked.Increment(ref _count);
}

