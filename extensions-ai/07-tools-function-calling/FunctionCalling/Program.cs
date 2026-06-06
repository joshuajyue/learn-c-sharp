// Lesson 07: Tools (a.k.a. function calling)
// ============================================================================
//
// WHY TOOLS EXIST
// ----------------------------------------------------------------------------
// An LLM only knows what it was trained on (often months-old) and can't run
// code. It can't look at TODAY'S weather, read your database, browse the web,
// or even reliably do arithmetic. To answer questions that require live or
// computed information, modern models support TOOL CALLS (a.k.a. FUNCTION
// CALLING):
//
//   * You hand the model a list of FUNCTIONS your code knows how to run --
//     each with a name, a one-line description, and a typed parameter list.
//   * When the model decides one of those functions could help, it doesn't
//     try to execute it. Instead it emits a STRUCTURED REQUEST: "please
//     call GetWeather({ city: \"Seattle\" }) for me."
//   * YOUR code runs the function and feeds the result back to the model.
//   * The model continues its answer with that result in hand.
//
// This is the core mechanism behind every "AI agent" you've seen -- the
// agent loop is just "let the model call tools until it stops asking".
//
//
// THE FOUR-STEP PROTOCOL (THE HARD WAY)
// ----------------------------------------------------------------------------
//   1. You put AIFunction objects in ChatOptions.Tools.
//   2. You call GetResponseAsync. The model returns a ChatResponse whose
//      Messages contain a FunctionCallContent (name + JSON-shaped args).
//   3. You execute the function in C# and append a new message of role
//      ChatRole.Tool containing a FunctionResultContent (the return value,
//      keyed back to the call by callId).
//   4. You call GetResponseAsync AGAIN with the longer history. Loop until
//      the response contains plain text (no more FunctionCallContent).
//
// You can write this loop yourself (and the toy client below shows what the
// model emits at each step), but it gets tedious fast -- especially when the
// model wants to chain several tool calls.
//
//
// THE FOUR-STEP PROTOCOL (THE EASY WAY)
// ----------------------------------------------------------------------------
// M.E.AI ships `UseFunctionInvocation()` in the `Microsoft.Extensions.AI`
// package. Wrap your client with it once and the wrapper runs the loop for
// you: you just `await GetResponseAsync(...)` and get the FINAL text back,
// with all the tool calls invoked behind the scenes. This is the version we
// use below.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI/Functions/FunctionInvokingChatClient.cs
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/Functions/AIFunctionFactory.cs
//
//
// KEY TYPES
// ----------------------------------------------------------------------------
//   AIFunction              -- a callable function the model can request.
//   AIFunctionFactory.Create-- builds an AIFunction from any C# method via
//                              reflection (name, description, parameter
//                              types all become tool metadata).
//   FunctionCallContent     -- "model wants to call <name> with <args>".
//   FunctionResultContent   -- "here's what <name> returned" (your reply).
//   ChatRole.Tool           -- the role you set on the message carrying a
//                              FunctionResultContent.
//
//
// WHY A FAKE MODEL?
// ----------------------------------------------------------------------------
// `WeatherCallingClient` below mimics the two-turn dance: on the FIRST call
// it emits a FunctionCallContent for "GetWeather"; on the SECOND call (which
// the function-invocation middleware makes for us, with the result appended)
// it emits the final natural-language answer. Reading this code is the best
// way to internalise what a real provider does on the wire.

using System.ComponentModel;
using Microsoft.Extensions.AI;

// Wrap the inner client with the function-invocation middleware. Building a
// `ChatClientBuilder` from an inner client and calling `.Build()` is the
// standard "compose an IChatClient pipeline" pattern (lesson 08 covers it
// in detail). For now: the wrapper produced by `.UseFunctionInvocation()`
// hides the call/result loop from the consumer.
IChatClient client = new ChatClientBuilder(new WeatherCallingClient())
    .UseFunctionInvocation()
    .Build();

// `AIFunctionFactory.Create(methodGroup, ...)` reflects on the method:
//   * Method name (or the `name:` override) and `[Description]` become the
//     tool metadata the model sees.
//   * Each parameter's name + `[Description]` tell the model how to fill it.
//   * The JSON args the model emits get bound back to the C# parameters by
//     name, with `System.Text.Json` doing the conversion.
//
// Tip: top-level local functions get compiler-mangled names like
// `<<Main>$>g__GetWeather|0_0`. ALWAYS pass an explicit `name:` so the
// model sees a stable, clean identifier. `GetWeather` (the bare name below
// the top-level statements) is a "method group" -- the C# compiler picks
// the right overload from context; here there's only one so it just refers
// to the method itself.
var weatherTool = AIFunctionFactory.Create(GetWeather, name: "GetWeather");

// `ChatOptions.Tools` is the list of functions THIS particular call may
// use. You can build it per-call (here) or set defaults via the builder.
// `[weatherTool]` is the C# 12 collection expression for a one-element list.
var options = new ChatOptions { Tools = [weatherTool] };

// The user just asks the question naturally. Under the hood:
//   - the wrapper sends the call,
//   - the toy model "decides" to invoke GetWeather("Seattle"),
//   - the wrapper invokes our C# GetWeather and feeds the result back,
//   - the toy model produces the final sentence.
// All FOUR steps of the protocol have happened by the time `await` returns.
var response = await client.GetResponseAsync(
    [ new(ChatRole.User, "What's the weather in Seattle?") ], options);

Console.WriteLine($"assistant > {response.Text}");


// `[Description("...")]` at method scope becomes the tool's one-line
// description in the schema. Parameter-level descriptions become parameter
// docs. Both end up in the JSON the model sees, so they materially affect
// how often the model picks the right tool with the right arguments.
//
// `static` on a local function forbids capturing variables (same explanation
// as lesson 05) and gives this method a clean signature for reflection.
//
// `=>` is the EXPRESSION-BODIED MEMBER syntax: `name(args) => expr;` is
// shorthand for `{ return expr; }`. Java's lambdas use the same arrow but
// only for functional-interface assignments; in C# it works for ordinary
// method bodies too.
[Description("Gets the current weather for a city.")]
static string GetWeather([Description("City name, e.g. Seattle")] string city) =>
    city.Equals("Seattle", StringComparison.OrdinalIgnoreCase)
        ? "53F and drizzly"
        : "sunny and 70F";


// --- A toy client that knows the function-calling protocol ------------------
//
// Reads the message list to decide which "phase" of the protocol it's in:
//   * If no FunctionResultContent is present yet, this is the FIRST call --
//     emit a FunctionCallContent to ask the host to run GetWeather.
//   * If a FunctionResultContent IS present, the wrapper has already invoked
//     the function and put the answer in the history -- produce the final
//     natural-language response that incorporates the result.
//
// A real LLM does the same reasoning but based on token probabilities, not
// `messages.Any(...)`.
internal sealed class WeatherCallingClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `messages.Any(m => m.Contents.OfType<FunctionResultContent>().Any())`
        // -- LINQ chain: for each message, project to its Contents
        // (`SelectMany`-ish but inside `Any`), filter to those that ARE
        // FunctionResultContent (`OfType<T>` is "filter + cast" combined),
        // then `Any()` to ask "is there at least one?" The Java equivalent
        // would chain `.stream().anyMatch(m -> m.getContents().stream()
        // .anyMatch(c -> c instanceof FunctionResultContent))`.
        bool alreadyCalled = messages.Any(m =>
            m.Contents.OfType<FunctionResultContent>().Any());

        ChatMessage msg;
        if (!alreadyCalled)
        {
            // Pretend the model decided to call GetWeather("Seattle").
            //
            // `callId` is a CORRELATION ID the model picks. The host echoes
            // it back on the matching FunctionResultContent so that
            // parallel/multi-tool conversations can pair up replies with
            // requests. Make it unique within a conversation.
            //
            // `arguments` is a `Dictionary<string, object?>` keyed by
            // parameter NAME (case-sensitive). Real models emit JSON; the
            // SDK parses it into this dictionary for us.
            //
            // `new Dictionary<...> { ["city"] = "Seattle" }` is an INDEXER
            // INITIALIZER -- a variant of the object initializer that calls
            // `this[key] = value` for each entry. Java analogue would be
            // `Map.of("city", "Seattle")` (immutable) or
            // `new HashMap<>() {{ put("city", "Seattle"); }}` (mutable).
            var call = new FunctionCallContent(
                callId: "call_1",
                name:   "GetWeather",
                arguments: new Dictionary<string, object?> { ["city"] = "Seattle" });

            // The assistant message carries the FunctionCallContent in its
            // `Contents`. A real model would set FinishReason = ToolCalls;
            // the function-invocation wrapper notices, runs the tool, and
            // calls us again.
            msg = new ChatMessage(ChatRole.Assistant, [ call ]);
        }
        else
        {
            // We're on the SECOND call. Find the FunctionResultContent the
            // wrapper appended and weave the result into a natural sentence.
            //
            // `SelectMany(m => m.Contents.OfType<FunctionResultContent>())`
            // flattens a sequence-of-sequences (each message's contents)
            // into a single sequence, while also filtering by type.
            // `.First()` returns the first element (throws if empty).
            // `.Result` is the value the host returned from the tool call.
            var result = messages
                .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
                .First().Result;

            msg = new ChatMessage(ChatRole.Assistant,
                $"It's currently {result} in Seattle.");
        }

        return Task.FromResult(new ChatResponse(msg) { FinishReason = ChatFinishReason.Stop });
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
