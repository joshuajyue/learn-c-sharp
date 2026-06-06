// Lesson 06: The middleware pipeline
//
// "Middleware" is the name ASP.NET Core gives to the chain of small pieces
// of code that EVERY request flows through before hitting your endpoint --
// and that EVERY response flows back through on the way out.
//
// Picture it as nested boxes around the endpoint:
//
//   request  -> [logging] -> [auth] -> [routing] -> [your endpoint] -> response
//                  ^           ^          ^                                |
//                  |           |          |       (response unwinds back)  |
//                  +-----------+----------+--------------------------------+
//
// Each piece of middleware gets the HTTP context and decides:
//   1. Do some work BEFORE the rest of the pipeline runs,
//   2. Call `next(context)` to keep going,
//   3. Optionally do some work AFTER the inner part finishes (e.g. logging
//      how long the request took, adding a response header),
//   OR -- it can SHORT-CIRCUIT by writing a response itself and NOT calling
//   `next`. The endpoint never runs in that case (e.g. auth says "you can't
//   come in here").
//
// ORDER MATTERS. You add middleware in the order it should execute on the
// way IN; the framework runs it in REVERSE on the way out. The recommended
// order for a typical app is:
//   ExceptionHandler -> HSTS -> HttpsRedirection -> Static Files ->
//   Routing -> CORS -> Authentication -> Authorization -> Endpoints

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// --- 1. Inline middleware via app.Use(...) ---
// Times every request and logs how long it took. The work AFTER `await next`
// runs on the way back out.
app.Use(async (ctx, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next(ctx);                              // run everything inner
    sw.Stop();
    Console.WriteLine($"[timing] {ctx.Request.Method} {ctx.Request.Path} -> " +
                      $"{ctx.Response.StatusCode} in {sw.ElapsedMilliseconds} ms");
});

// --- 2. Short-circuiting middleware ---
// If the URL contains "block", write 403 Forbidden and DO NOT call next --
// the endpoint won't run, the request stops here. This is exactly how auth
// middleware refuses an unauthenticated request.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Value?.Contains("block", StringComparison.OrdinalIgnoreCase) == true)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsync("blocked by middleware");
        return;          // <-- no next(ctx) -> pipeline terminates here
    }
    await next(ctx);
});

// --- 3. Custom middleware as a class (convention-based) ---
// When middleware has dependencies or shared state, use a class. The host
// creates ONE instance for the whole app and reuses it on every request,
// so don't store request-specific state in fields -- use HttpContext.Items.
app.UseMiddleware<CorrelationIdMiddleware>();

// --- Endpoints (run after all middleware on the inbound trip) ---
app.MapGet("/", (HttpContext ctx) => new
{
    correlationId = ctx.Items["CorrelationId"],
    message = "pipeline complete"
});
app.MapGet("/block-me", () => "you should never see this");

app.Run();

// Convention-based middleware: a class with an `InvokeAsync(HttpContext, RequestDelegate)`
// method. `next` is the rest of the pipeline -- await it to continue, skip it
// to short-circuit.
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // A "correlation id" is a unique identifier per request that you log
        // alongside everything you do, so you can trace one request's path
        // through your logs. Read it from the incoming header if the caller
        // provided one; otherwise generate a fresh one.
        var id = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N");

        // HttpContext.Items is a per-request bag of properties later
        // middleware and endpoints can read.
        context.Items["CorrelationId"] = id;

        // Echo the id back so the client can also log it.
        context.Response.Headers["X-Correlation-Id"] = id;

        await next(context);          // pass control to the next thing inward
    }
}

