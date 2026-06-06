// Lesson 09: DI, the Host, and swapping in a real provider
// ============================================================================
//
// WHY DI MATTERS FOR M.E.AI
// ----------------------------------------------------------------------------
// Up to now every lesson `new`d an IChatClient at the top of the file and
// passed it around by hand. That works for one-file demos. Real applications
// have dozens of classes that all want to talk to the LLM, and you do NOT
// want each of them constructing its own client (different middleware order
// per call site, leaked sockets, untestable code...).
//
// The .NET answer is the SAME answer it gives for every "shared service"
// problem: register the thing once in a DEPENDENCY INJECTION (DI) container,
// then have your classes ASK for it via constructor parameters. The
// container hands the same configured instance to everyone who needs one.
//
// Console lesson 15 walked through Microsoft.Extensions.DependencyInjection
// + Microsoft.Extensions.Hosting from scratch. This lesson assumes you know
// the basics (Host.CreateApplicationBuilder, IServiceCollection, ctor
// injection) and shows how IChatClient plugs into the same machinery.
//
//
// THE REGISTRATION PATTERN
// ----------------------------------------------------------------------------
//   builder.Services.AddChatClient(<inner client OR factory>)
//          .UseFunctionInvocation()        // (lesson 07)
//          .UseLogging(loggerFactory)
//          .UseDistributedCache(cache)     // (lesson 15)
//          .UseOpenTelemetry(...);
//
// `AddChatClient(...)` returns a `ChatClientBuilder` -- the same type you
// used directly in lesson 08 -- so all the same `.Use*` extensions work.
// Once registered, anything in the DI container can ASK for `IChatClient`
// via a constructor parameter and receive the fully-composed pipeline.
//
//
// SWAPPING IN A REAL PROVIDER
// ----------------------------------------------------------------------------
// The fake we register below is a placeholder. To talk to a real model in
// production, you replace ONLY the inner client. Each provider ships its
// own NuGet package; the conversion call exposes an IChatClient:
//
//     // OpenAI / Azure OpenAI
//     builder.Services.AddChatClient(
//         new OpenAIClient(apiKey).GetChatClient("gpt-4o-mini").AsIChatClient());
//
//     // Local Ollama (no API key required)
//     builder.Services.AddChatClient(
//         new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2"));
//
// Same registration, same downstream code. The `JokeAgent` class below has
// NO IDEA which provider is on the other end. That portability is the
// entire point of M.E.AI -- the value isn't the abstractions per se, it's
// being able to swap providers without rewriting your app.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ServiceCollectionChatClientBuilderExtensions.cs
//
//
// WHY A FAKE CLIENT?
// ----------------------------------------------------------------------------
// The whole lesson is about the registration/injection PATTERN. The fake
// just produces a canned joke so we can see the agent actually get called.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// `Host.CreateApplicationBuilder(args)` is the modern generic-host entrypoint.
// It wires up configuration, logging, and the DI container. `args` is the
// implicit `string[] args` provided to top-level statements. (Yes, even
// without a `Main` method declaration, the magic `args` array exists --
// the compiler synthesises the parameter on the hidden Main it generates.)
var builder = Host.CreateApplicationBuilder(args);

// Register IChatClient on the DI container and chain middleware on top.
// `AddChatClient(innerClient)` records the inner-most client; each
// subsequent `.UseXxx` wraps a layer around it (lesson 08).
//
// You can swap the FakeChatClient line for any real provider; everything
// else in this file -- the JokeAgent class and the consuming code -- stays
// identical. THIS LINE is the only place that knows which model is in use.
builder.Services
    .AddChatClient(new FakeChatClient())
    .UseFunctionInvocation();

// Register our consuming type. `AddSingleton<T>` registers T as a singleton
// resolved from its constructor; the container will hand it the IChatClient
// we just registered. `AddScoped` / `AddTransient` are the other lifetimes
// (same vocabulary as Spring's @Scope("singleton" / "request" / "prototype")).
builder.Services.AddSingleton<JokeAgent>();

// `using var host = builder.Build();` -- the USING DECLARATION (C# 8). The
// variable lives until the end of the enclosing scope, and `Dispose` is
// called automatically when that scope exits. Equivalent to wrapping
// everything below in `using (var host = builder.Build()) { ... }`.
// Hosts hold disposable infrastructure (loggers, DI container, hosted
// services); disposing cleanly shuts them down.
using var host = builder.Build();

// Resolve a service from the container. `GetRequiredService<T>` throws if
// T isn't registered (vs. `GetService<T>` which returns null). In a normal
// app you wouldn't manually resolve at startup -- you'd register an
// `IHostedService` that gets started by `host.RunAsync()` -- but for a
// console demo this is the smallest path to "actually call the agent".
var agent = host.Services.GetRequiredService<JokeAgent>();
Console.WriteLine(await agent.GetJokeAsync("dotnet"));


// --- A service that consumes IChatClient via constructor injection ---------
//
// Primary constructor (C# 12): `(IChatClient chat)` becomes a constructor
// parameter that is in scope throughout the class body. The DI container
// finds the public ctor, sees the `IChatClient` parameter, and supplies the
// one we registered above. Same idea as Spring's `@Autowired` constructor
// injection but built into the language instead of an annotation.
//
// This class is the WHOLE POINT of DI: it has no idea whether `chat` is a
// fake, an OpenAI client, an Ollama client, or a wrapped/cached/logged
// version of any of those. It just speaks the IChatClient interface.
internal sealed class JokeAgent(IChatClient chat)
{
    public async Task<string> GetJokeAsync(string topic)
    {
        var response = await chat.GetResponseAsync(
            [
                new(ChatRole.System, "You tell short one-liner jokes."),
                new(ChatRole.User,   $"Tell me a joke about {topic}."),
            ]);
        return response.Text;
    }
}


// --- The fake provider we registered ---------------------------------------
//
// Same shape as every other IChatClient implementation in this track --
// just with a punchier reply. In production you'd delete this class and use
// `new OpenAIClient(...)` (or another provider) on the AddChatClient line.
internal sealed class FakeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Lift the topic out of the user's prompt. `LastOrDefault` returns
        // null if no user message exists; `?.Text` short-circuits past null
        // (lesson 01); `?? "things"` supplies a default if both are null.
        var userText = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "things";

        // `const string` is a COMPILE-TIME constant -- inlined at every call
        // site. Java equivalent: `private static final String`.
        const string prefix = "Tell me a joke about ";

        // `userText[prefix.Length..]` -- range-expression substring from
        // index `prefix.Length` to the end. `.TrimEnd('.', '!', '?', ' ')`
        // strips any trailing punctuation. The ternary picks "the topic" if
        // we recognised the prefix, otherwise just echoes the whole text.
        var topic = userText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? userText[prefix.Length..].TrimEnd('.', '!', '?', ' ')
            : userText;

        var joke = $"Why did the {topic} cross the road? To get to the other side of the abstraction.";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, joke)));
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
