// Lesson 01: Hello, IChatClient
// ============================================================================
//
// WHAT IS AN "LLM CHAT", MECHANICALLY?
// ----------------------------------------------------------------------------
// A Large Language Model (LLM) like GPT-4 is, at the API level, a pure
// function:
//
//     text in  --->  text out
//
// You give it a sequence of messages ("the conversation so far") and it
// returns one more message ("what the assistant would say next"). The model
// itself is STATELESS -- it does not remember anything between calls. If you
// want multi-turn chat, YOUR code keeps the message list and re-sends it on
// every call. (Lesson 03 walks through that.)
//
//
// WHAT IS Microsoft.Extensions.AI (M.E.AI)?
// ----------------------------------------------------------------------------
// `Microsoft.Extensions.AI` is the same kind of "one interface, many
// providers" play that Microsoft.Extensions.Logging or
// Microsoft.Extensions.DependencyInjection are. Java analogue: SLF4J --
// you code against one interface, then plug in whichever backend you want.
// Where `ILogger` lets you write code that doesn't care if it ends up in
// console / file / Application Insights, `IChatClient` lets you write code
// that doesn't care if it talks to OpenAI / Azure OpenAI / Ollama (local) /
// Anthropic / a fake in-memory client.
//
//
// THE CENTRAL ABSTRACTION
// ----------------------------------------------------------------------------
//   public interface IChatClient : IDisposable
//   {
//       Task<ChatResponse> GetResponseAsync(
//           IEnumerable<ChatMessage>, ChatOptions?, CancellationToken);
//       IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(...);
//       object? GetService(Type, object? key);
//   }
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/IChatClient.cs
//
// `IDisposable` is C#'s "release native/unmanaged resources" hook -- same
// idea as Java's `AutoCloseable`. The `using` STATEMENT (different from the
// `using` DIRECTIVE at the top of a file!) auto-calls `Dispose` at scope exit.
//
//
// MENTAL MODEL FOR A "CHAT"
// ----------------------------------------------------------------------------
//   * You send a LIST of ChatMessages -- each has a ROLE and CONTENT.
//   * Roles:
//       - System     : standing instructions / persona
//                      ("you are a helpful tutor; answer briefly").
//       - User       : the human's turn.
//       - Assistant  : the model's PRIOR replies, fed back so it sees its
//                      own history (remember: the model is stateless).
//       - Tool       : results from a tool/function call (lesson 05).
//   * The model returns ONE new assistant message wrapped in a `ChatResponse`.
//   * "Tokens" (you'll see `Usage.InputTokenCount` etc. below) are the
//     model's subword units -- roughly ~4 characters or ~3/4 of an English
//     word. Token counts matter because providers bill by token AND every
//     model has a hard context-window limit measured in tokens.
//
//
// WHY A FAKE CLIENT FOR LESSON 01?
// ----------------------------------------------------------------------------
// To keep this lesson runnable WITHOUT an API key or network, we implement
// `IChatClient` ourselves at the bottom of this file (`EchoChatClient`).
// Later lessons swap in a real provider; the consumer code at the top of
// this file does NOT change. That portability is the whole point.

// `using` (as a DIRECTIVE, at file scope) imports a namespace so we can
// write `IChatClient` instead of `Microsoft.Extensions.AI.IChatClient`.
// Java analogue: `import com.foo.Bar;`. (Confusingly, `using` is also the
// keyword for the disposable-scope STATEMENT -- different feature, same word.)
using Microsoft.Extensions.AI;

// Notice there is NO `class Program` and NO `static void Main` in this file.
// This is "top-level statements" (C# 9+): the compiler wraps the loose
// statements below in a hidden `Main` method. Only ONE file per project may
// use this style. Because we `await` further down, that hidden `Main` is
// implicitly `async Task Main()`. Java has no equivalent -- you always need
// an explicit `public static void main(String[] args)`.

// Declare the variable using the INTERFACE type and assign a concrete impl.
// Same shape as Java: `IChatClient client = new EchoChatClient();`. Doing
// this (rather than `var`) emphasises that consumer code only depends on
// the interface -- swap the right-hand side for OpenAI/Ollama/etc. and
// nothing else in this file changes.
IChatClient client = new EchoChatClient();

// `var` = "infer the static type from the right-hand side". This is NOT
// dynamic typing; the type is fixed at compile time, you just don't have to
// repeat it. Closer to Java 10's `var` than to JavaScript's `var`.
//
// `new List<ChatMessage> { ... }` is a COLLECTION INITIALIZER -- sugar for
// constructing the list and then calling `.Add(...)` on each element inside
// the braces. Result is `List<ChatMessage>` (≈ Java `ArrayList<ChatMessage>`).
//
// Inside, `new(ChatRole.System, "...")` is a TARGET-TYPED `new` (C# 9+):
// the compiler already knows the element type must be `ChatMessage` (from
// the list's generic argument), so we omit the type name. Equivalent long
// form would be:
//     new ChatMessage(ChatRole.System, "You are a brief assistant.")
var messages = new List<ChatMessage>
{
    // SYSTEM message = the model's "standing orders". Sent every turn.
    // Good system prompts make a HUGE difference to model behaviour.
    new(ChatRole.System, "You are a brief assistant."),
    // USER message = what the human just typed.
    new(ChatRole.User,   "Hello, who are you?"),
};

// `await` unwraps a `Task<T>` into a `T`, suspending THIS method until the
// task completes -- without blocking the OS thread. Closer to JavaScript's
// `await` than to Java's `Future.get()` (which blocks the calling thread).
// `GetResponseAsync` returns `Task<ChatResponse>`; after `await` we hold
// the unwrapped `ChatResponse` value.
ChatResponse response = await client.GetResponseAsync(messages);

// `$"..."` is an INTERPOLATED STRING. Any `{expr}` inside is evaluated and
// formatted into the string. Java analogue: `String.format("%s", expr)` or
// the newer `"%s".formatted(expr)`. C in spirit: `printf("%s", expr)`.
//
// `ChatResponse.Text` is a convenience that concatenates the text content
// of every message in the response (usually just one). The full structured
// list lives in `ChatResponse.Messages` -- you need it when a response
// contains multiple parts (e.g. tool calls + text -- see lesson 05).
Console.WriteLine($"assistant > {response.Text}");

// FinishReason explains WHY the model stopped generating:
//   Stop          = it decided it was done (the normal, healthy case).
//   Length        = it hit `max_tokens`; the output was truncated mid-thought.
//   ContentFilter = a safety system blocked the output.
//   ToolCalls     = it wants to call a tool/function (lesson 05).
Console.WriteLine($"finish    : {response.FinishReason}");

// `?.` is the NULL-CONDITIONAL operator. If `response.Usage` is null, the
// whole expression short-circuits to null (instead of throwing a
// NullReferenceException) and the interpolated string just prints empty.
// Same shape as Kotlin's `?.` or Groovy's safe-navigation. Java has no
// direct equivalent (you'd reach for `Optional` or a manual null check).
//
// Real providers usually fill `Usage` (so you can track cost). Cheap mocks
// or local models sometimes leave it null -- hence the defensive `?.`.
Console.WriteLine($"input tok : {response.Usage?.InputTokenCount}");
Console.WriteLine($"output tok: {response.Usage?.OutputTokenCount}");


// --- A toy IChatClient implementation --------------------------------------
//
// Real implementations live in NuGet packages like:
//   Microsoft.Extensions.AI.OpenAI       (OpenAI / Azure OpenAI)
//   OllamaSharp                          (local Ollama models, no API key)
// They all implement this exact interface, so the consumer code at the top
// of this file is portable across providers.
//
// Modifiers on the class line:
//   `internal` = visible only inside this assembly (project). Java has no
//                exact equivalent; "package-private" is the closest cousin.
//                Use this for impl details you don't want to expose.
//   `sealed`   = cannot be subclassed. Same meaning as Java `final` on a
//                class. Worth using on impls you don't intend to extend --
//                the JIT can devirtualize calls, and intent is clearer.
//   `: IChatClient` = "implements IChatClient" (Java would use `implements`,
//                C# uses the same `:` symbol for both `extends` and `implements`).
internal sealed class EchoChatClient : IChatClient
{
    // OPTIONAL PARAMETERS with default values: `options = null`,
    // `cancellationToken = default`. Java has no equivalent -- you'd write
    // multiple overloads. C# also supports NAMED arguments at call sites,
    // e.g. `GetResponseAsync(messages, cancellationToken: ct)`.
    //
    // `default` keyword = "the default value of this type". For reference
    // types it's null; for value types (`struct`s like `CancellationToken`)
    // it's the zeroed struct. Useful as a placeholder when you don't care.
    //
    // `IEnumerable<T>` = the most basic "lazily iterable sequence of T".
    // Java analogue: `Iterable<T>`. LINQ methods (`.Sum`, `.LastOrDefault`,
    // `.Where`, etc.) extend `IEnumerable<T>` -- comparable to Java Streams,
    // but generally simpler to read.
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Decompose the chained expression below piece by piece:
        //
        //   messages.LastOrDefault(m => m.Role == ChatRole.User)
        //     ^ LINQ: returns the LAST element matching the lambda predicate,
        //       or `default(ChatMessage?)` (= null) if none matched.
        //       Java Streams analogue: .filter(...).reduce((a,b) -> b).orElse(null).
        //       `m => m.Role == ChatRole.User` is a LAMBDA -- same shape as Java.
        //
        //   ?.Text
        //     ^ null-conditional: if the message is null, skip the property
        //       access and yield null instead of throwing.
        //
        //   ?? "(nothing)"
        //     ^ null-COALESCING operator: "use the left if non-null, else the
        //       right". Java analogue: Optional.ofNullable(x).orElse("(nothing)").
        var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "(nothing)";

        // `\"` inside an interpolated string is an escaped double-quote --
        // same escape rules as Java/C string literals.
        var reply = $"You said: \"{lastUser}\". I'm a fake IChatClient.";

        // OBJECT INITIALIZER syntax: after the constructor call, the `{ ... }`
        // block sets writable properties on the freshly-constructed object.
        // It's pure sugar for:
        //     var r = new ChatResponse(new ChatMessage(ChatRole.Assistant, reply));
        //     r.FinishReason = ChatFinishReason.Stop;
        //     r.Usage = new UsageDetails { /* ... */ };
        // Java has no built-in equivalent -- you'd use a builder, a record
        // constructor with many args, or post-construction setters.
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, reply))
        {
            FinishReason = ChatFinishReason.Stop,
            Usage = new UsageDetails
            {
                // NOTE: real tokenizers are not character counts! This is a
                // toy heuristic just to populate the fields. OpenAI's rule of
                // thumb is ~4 chars per token for English. Real providers
                // return the EXACT counts they billed you for.
                //
                // `messages.Sum(m => m.Text?.Length ?? 0)` = LINQ Sum with a
                // selector lambda. Equivalent to a foreach + accumulator loop.
                InputTokenCount  = messages.Sum(m => m.Text?.Length ?? 0) / 4,
                OutputTokenCount = reply.Length / 4,
                TotalTokenCount  = 0,
            },
        };

        // Our work is purely synchronous (no I/O), but the interface demands
        // a `Task<ChatResponse>` return. `Task.FromResult(x)` wraps an
        // already-known value in an already-completed Task -- the cheap way
        // to satisfy an async signature from synchronous code.
        return Task.FromResult(response);
    }

    // EXPRESSION-BODIED MEMBER: `member(...) => expr;` is shorthand for a
    // method whose body is a single expression. Equivalent long form would be:
    //     { throw new NotImplementedException("See lesson 02 for streaming."); }
    //
    // Streaming returns an `IAsyncEnumerable<T>` -- think "an async-friendly
    // sequence of pieces of the answer as they arrive over the network".
    // You consume it with `await foreach (var chunk in stream) { ... }`.
    // It's how chat UIs render tokens one-by-one. Covered properly in lesson 02.
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("See lesson 02 for streaming.");

    // `GetService` is the M.E.AI way of unwrapping the pipeline: callers ask
    // "do you (or anyone you wrap) expose this type?" -- useful for getting
    // `ChatClientMetadata`, telemetry hooks, the underlying SDK client, etc.
    // Returning null means "no". You'll see this pattern again in lesson 06
    // where we stack middleware around a client.
    //
    // Body breakdown:
    //   `typeof(IChatClient)` = the runtime `Type` object for the interface
    //                           (Java analogue: `IChatClient.class`).
    //   `serviceKey is null`  = PATTERN MATCHING. `x is <pattern>` returns
    //                           bool. `is null` is the modern, idiomatic
    //                           way to null-check in C# (composes naturally
    //                           with richer patterns like `is string s` or
    //                           `is { Length: > 0 }`).
    //   `cond ? a : b`        = ternary conditional, same as Java/C.
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    // Required by `IDisposable`. We hold no unmanaged resources (no sockets,
    // no file handles, no native memory), so this is an empty no-op. Real
    // HTTP-backed clients would close their `HttpClient` here. Java analogue:
    // implementing `AutoCloseable.close()` with an empty body.
    public void Dispose() { }
}
