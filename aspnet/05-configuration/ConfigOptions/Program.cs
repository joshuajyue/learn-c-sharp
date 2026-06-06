// Lesson 05: Configuration + IOptions<T>
//
// "Configuration" is values your app reads at startup that you DON'T want
// hard-coded -- things like database connection strings, feature toggles,
// API keys, log levels, port numbers. Different environments (your laptop,
// staging, production) want different values without recompiling.
//
// ASP.NET Core layers multiple SOURCES into one logical config object, in
// this order (each later one overrides the previous):
//
//   1. appsettings.json                            (committed, shipped with the app)
//   2. appsettings.{Environment}.json              (Development / Staging / Production)
//   3. User secrets                                (Development only -- per-dev secrets)
//   4. Environment variables                       (per-machine / per-deployment)
//   5. Command-line args                           (--Key=Value when you `dotnet run`)
//
// Result: production secrets stay out of source control (#4 / #5), local
// development can override safely (#2 / #3), and the JSON file is the
// single source of defaults.
//
// You CAN read flatly: `IConfiguration["Greeter:Prefix"]`. The IDIOMATIC
// pattern, though, is to BIND a config section to a strongly-typed class
// once and inject it as IOptions<T> wherever it's needed. Three flavours:
//
//   IOptions<T>          -- bound at startup, never changes during the app
//   IOptionsSnapshot<T>  -- re-bound at the START of each request (scoped)
//   IOptionsMonitor<T>   -- re-bound on file change + callback support
//
// Try:
//   curl http://localhost:5xxx/greet
//
// Override the prefix via an env var (note the double underscore: that's how
// nested JSON paths are written in env-var form):
//   PowerShell:  $env:Greeter__Prefix = "Howdy"; dotnet run

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Bind the "Greeter" SECTION of appsettings.json to GreeterOptions. The
// container will create an IOptions<GreeterOptions> with .Value filled in
// from whatever config sources won the override race.
builder.Services.Configure<GreeterOptions>(builder.Configuration.GetSection("Greeter"));

var app = builder.Build();

// IOptions<T> is injected the same way as any other service (lesson 04).
// .Value is the bound POCO.
app.MapGet("/greet", (IOptions<GreeterOptions> opts) =>
{
    var o = opts.Value;
    var msg = $"{o.Prefix}, {o.Audience}!";
    return o.Loud ? msg.ToUpperInvariant() : msg;
});

// Reading config flatly + showing which environment ASP.NET thinks it's in.
// IHostEnvironment.EnvironmentName comes from the ASPNETCORE_ENVIRONMENT
// env var (default: "Production"; `dotnet run` sets "Development").
app.MapGet("/env", (IConfiguration cfg, IHostEnvironment env) => new
{
    environment   = env.EnvironmentName,
    contentRoot   = env.ContentRootPath,
    customSetting = cfg["MyApp:Setting"] ?? "(unset)",
});

app.Run();

public sealed class GreeterOptions
{
    public string Prefix   { get; set; } = "Hi";
    public string Audience { get; set; } = "world";
    public bool   Loud     { get; set; }
}

