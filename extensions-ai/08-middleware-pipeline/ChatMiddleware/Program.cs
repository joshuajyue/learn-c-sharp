// Lesson 08: The IChatClient middleware pipeline
// ============================================================================
//
// THE BIG IDEA: ONIONS
// ----------------------------------------------------------------------------
// Every cross-cutting concern around an LLM call -- logging, retries,
// caching, telemetry, function invocation -- is the SAME PATTERN: "do
// something before and/or after the next thing in the chain runs". That's
// middleware. You already saw it in ASP.NET Core (aspnet/06):
//
//                     [request]   --->   --->   --->  inner handler
//                                                       (runs)
//                     [response]  <---   <---   <---
//
// M.E.AI exposes exactly the same nesting for chat calls. You wrap an
// IChatClient with OTHER IChatClient instances that DELEGATE to the inner
// one. Composing them is the job of `ChatClientBuilder`:
//
//     IChatClient client = new ChatClientBuilder(realModel)
//         .UseFunctionInvocation()        // lesson 07
//         .UseLogging(loggerFactory)
//         .UseDistributedCache(cache)     // lesson 15
//         .UseOpenTelemetry(...)
//         .Use(next => new MyCustom(next))
//         .Build();
//
// Each `Use*` method appends another LAYER. The OUTERMOST (last-added) layer
// sees the call FIRST on the way in and LAST on the way out. Order matters:
// a cache placed OUTSIDE retries means cache hits skip retries (good); a
// cache placed INSIDE retries would retry around the cache (probably wrong).
//
//
// THE BASE TYPE: DelegatingChatClient
// ----------------------------------------------------------------------------
// To write your own middleware, derive from `DelegatingChatClient`. It
// stores the inner client and forwards every interface method to it. You
// OVERRIDE only the methods you want to intercept; the rest forward as-is.
// Exactly the same shape as `DelegatingHandler` for HttpClient.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatClientBuilder.cs
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/DelegatingChatClient.cs
//
//
// MENTAL MODEL OF THE PIPELINE WE BUILD BELOW
// ----------------------------------------------------------------------------
// We add layers in this order:   `.Use(TimingClient).Use(RetryOnceClient)`
// At runtime each call traverses:
//
//     [user call]
//        |
//        v
//     RetryOnceClient.GetResponseAsync     <-- OUTERMOST (last added)
//        |   try {
//        v
//     TimingClient.GetResponseAsync        <-- middle layer
//        |   sw = Stopwatch.StartNew();
//        v
//     EchoClient.GetResponseAsync          <-- INNERMOST (the "model")
//        |   prints "[inner ] generating response"
//        v
//     (return travels back up; TimingClient prints elapsed; RetryOnceClient
//      catches any exception and retries once)
//
// You'll see the [inner ] log first, then [timing], in the program output.
//
//
// WHY A FAKE MODEL?
// ----------------------------------------------------------------------------
// Same reason as every lesson: no API key, no network. The lesson is about
// the PIPELINE, not the model -- `EchoClient` just returns "pong" so we can
// see the middleware around it actually firing.

// `System.Diagnostics` is the BCL namespace for tracing/diagnostics types,
// including `Stopwatch` (a high-resolution timer that wraps QueryPerformanceCounter).
using System.Diagnostics;
using Microsoft.Extensions.AI;

IChatClient inner = new EchoClient();

// `.Use(...)` takes a FACTORY: `Func<IChatClient, IChatClient>` that, given
// the inner client, returns a wrapper. This is the "anonymous middleware"
// pattern -- equivalent to ASP.NET Core's `app.Use((ctx, next) => ...)`.
//
// `next => new TimingClient(next)` is a LAMBDA; it gets called by the
// builder during `.Build()`, with the previously-composed pipeline as
// `next`. Read the two .Use calls bottom-up to see why RetryOnceClient
// ends up OUTERMOST: it was added LAST, so it gets to wrap everything
// that came before it.
IChatClient client = new ChatClientBuilder(inner)
    .Use(next => new TimingClient(next))
    .Use(next => new RetryOnceClient(next))
    .Build();

// Plain old call -- the consumer doesn't know or care about the pipeline.
// That's the whole point: middleware is composable infrastructure.
var resp = await client.GetResponseAsync(
    [ new(ChatRole.User, "ping") ]);

Console.WriteLine();
Console.WriteLine($"final > {resp.Text}");


// --- Middleware #1: time every call ----------------------------------------
//
// PRIMARY CONSTRUCTOR (C# 12) -- `class Foo(IChatClient inner)` declares a
// constructor parameter that is in scope throughout the class body. Java has
// no exact equivalent (records have a positional ctor but for classes you'd
// write the ctor and field by hand). Here we pass `inner` straight to the
// base type's primary ctor (`: DelegatingChatClient(inner)`) so the base
// class stores it and forwards calls.
//
// We OVERRIDE only `GetResponseAsync` -- everything else (streaming,
// GetService, Dispose) forwards to the inner client unchanged.
internal sealed class TimingClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `Stopwatch.StartNew()` returns a started stopwatch. Equivalent
        // long form: `var sw = new Stopwatch(); sw.Start();`. Java analogue
        // would be `long start = System.nanoTime()` and a subtract later.
        var sw = Stopwatch.StartNew();

        // `base.GetResponseAsync(...)` calls the DelegatingChatClient's
        // forwarder, which calls the next inner client. Returning the same
        // `ChatResponse` keeps the response shape unchanged for callers.
        var resp = await base.GetResponseAsync(messages, options, cancellationToken);

        sw.Stop();
        Console.WriteLine($"[timing] call took {sw.ElapsedMilliseconds} ms");
        return resp;
    }
}

// --- Middleware #2: retry once on failure ----------------------------------
//
// Retries are the canonical middleware example. We catch ANY exception,
// log it, and try once more. Real-world retries are pickier (only HTTP
// 5xx / 429, with exponential backoff and jitter; M.E.AI has a `.UseRetry`
// helper for that). The shape is the lesson.
internal sealed class RetryOnceClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        // `catch (Exception ex)` -- C#'s catch clause names the type and an
        // optional variable. Java is syntactically identical.
        catch (Exception ex)
        {
            // `ex.GetType().Name` -- runtime type name (e.g. "HttpRequestException").
            // Equivalent to Java's `ex.getClass().getSimpleName()`.
            Console.WriteLine($"[retry] first attempt failed ({ex.GetType().Name}); retrying once");
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
    }
}

// --- Innermost: the actual "model" -----------------------------------------
//
// Logs that it ran so you can see the call traversal order on stdout. A
// real model would do the inference; our placeholder always returns "pong".
internal sealed class EchoClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[inner ] generating response");
        return Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "pong")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
