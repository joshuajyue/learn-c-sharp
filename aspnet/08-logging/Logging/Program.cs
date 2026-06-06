// Lesson 08: Structured logging with ILogger<T>
//
// A web server can handle thousands of requests per second across many
// threads. The only way to understand what it's doing is to LOG -- write
// messages somewhere you can read them later (console during development,
// files / cloud log services in production).
//
// "Structured" logging means the log message ISN'T just a finished string --
// it carries the values as named PROPERTIES alongside the template, so log
// tools can index/filter on them. For example:
//
//   log.LogInformation("User {UserId} did {Action}", 7, "login");
//
// gets stored with three things: the template, UserId=7, Action="login". A
// log viewer can then answer "show me everything UserId==7 did today" with
// a query, not a regex over text.
//
// Rules of thumb:
//   * Inject ILogger<T> where T is the class doing the logging. T's full
//     name becomes the "category" -- a way to filter logs by area.
//   * Use named placeholders in the template, pass values as separate args.
//   * NEVER do `log.LogInformation($"hello {name}")` with an interpolated
//     string -- you lose the structured property AND pay the formatting
//     cost even when the log level is filtered out.
//
// Levels, low to high importance:
//   Trace -> Debug -> Information -> Warning -> Error -> Critical
// "Information" is the default minimum; Debug/Trace are suppressed unless
// you turn them on in appsettings.json.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// --- 1. Inject a logger into an endpoint ---
// `ILogger<Program>` -> the log entries are tagged with category "Program".
app.MapGet("/", (ILogger<Program> log) =>
{
    log.LogInformation("Hit / at {Time}", DateTimeOffset.UtcNow);
    return "hello";
});

// --- 2. Structured properties in action ---
// UserId and Action are captured separately. A log query tool can answer
// "show me all logins by user 7" without parsing the message string.
app.MapPost("/users/{userId:int}/action", (int userId, string action, ILogger<Program> log) =>
{
    log.LogInformation("User {UserId} did {Action}", userId, action);
    return Results.Accepted();
});

// --- 3. Scopes -- attach extra properties to every log inside a block ---
// Typical use: tag every log in a request with a request id, tenant id, etc.
// Anything inside the `using` will carry "RequestId=..." automatically.
app.MapGet("/scoped", (ILogger<Program> log) =>
{
    using (log.BeginScope("RequestId={RequestId}", Guid.NewGuid().ToString("N")))
    {
        log.LogInformation("inside scope");
        log.LogWarning("still inside scope");
    }
    log.LogInformation("outside scope -- no RequestId attached here");
    return "ok";
});

// --- 4. Levels & guarding expensive formatting ---
app.MapGet("/levels", (ILogger<Program> log) =>
{
    log.LogTrace("trace");          // suppressed by default
    log.LogDebug("debug");          // suppressed by default
    log.LogInformation("info");
    log.LogWarning("warn");
    log.LogError("error (not a real one)");

    // If building the value is expensive, check IsEnabled first so you don't
    // do the work when the level is off.
    if (log.IsEnabled(LogLevel.Debug))
        log.LogDebug("expensive: {Snapshot}", BuildExpensiveSnapshot());

    return "see console output";
});

app.Run();

static string BuildExpensiveSnapshot() => "snapshot";

