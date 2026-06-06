// Lesson 02: Streaming responses
// ============================================================================
//
// WHY STREAMING EXISTS
// ----------------------------------------------------------------------------
// A real LLM doesn't compute the whole answer atomically and hand it back.
// Internally it produces TOKENS (sub-word units, roughly 3/4 of an English
// word each) one at a time, in a loop:
//
//     while (not done) { pick the next token given everything so far }
//
// A multi-paragraph answer can take many seconds to finish that loop. If your
// program just `await`s the full response (lesson 01), the user sees a blank
// screen for the entire duration. STREAMING fixes that: the provider flushes
// each token over the wire as soon as the model emits it, and your code
// renders the partial answer LIVE. That's how ChatGPT's "typing" effect
// works, and it's expected UX for any chat surface today.
//
// Streaming is also useful for LONG generations -- summaries, code, articles.
// You can start showing output (and stop early) without waiting for the
// model's "I'm done" signal.
//
//
// THE STREAMING METHOD
// ----------------------------------------------------------------------------
// IChatClient exposes two complementary methods. Lesson 01 used the first;
// lesson 02 uses the second:
//
//   Task<ChatResponse>                  GetResponseAsync(...)          // whole
//   IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(...) // chunks
//
// Note the return types:
//
//   * `Task<ChatResponse>`             -- one future value (Java: CompletableFuture<T>).
//   * `IAsyncEnumerable<ChatResponseUpdate>` -- a SEQUENCE of values that
//                                         arrive over time, each retrieved
//                                         asynchronously. Java has no direct
//                                         equivalent in the JDK; Reactor's
//                                         `Flux<T>` is the closest cousin.
//
// You consume the second one with `await foreach`, the streaming variant of
// `foreach`. Each iteration awaits the next chunk.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/IChatClient.cs
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseUpdate.cs
//
//
// MENTAL MODEL FOR A STREAM
// ----------------------------------------------------------------------------
//   * Each ChatResponseUpdate carries a SLICE of the final answer. The most
//     common payload is some text in `.Text`; you concatenate slices to
//     reconstruct the full message.
//   * Only the FIRST update is guaranteed to set the Role (Assistant). Later
//     updates often leave it null -- the consumer merges them.
//   * Updates can also carry FUNCTION-CALL pieces, USAGE info at the end, etc.
//     For pure-text streams (this lesson) you just print `.Text`.
//
// If you only need the final aggregated ChatResponse (same shape lesson 01
// returned), there is a one-line extension that drains the stream for you:
//
//     ChatResponse full = await stream.ToChatResponseAsync();
//
// (from ChatResponseExtensions in M.E.AI.Abstractions). Convenient, but
// destroys the whole reason for streaming -- the incremental UX. So when
// you DO want the typewriter effect, drive the `await foreach` yourself.
//
//
// WHY A FAKE CLIENT?
// ----------------------------------------------------------------------------
// Same deal as lesson 01: we want the lesson to run with `dotnet run` and no
// API key. `TypewriterChatClient` below yields one word at a time with a tiny
// delay so you can actually see the streaming on stdout. The consumer code at
// the top of this file is exactly what you'd write against a real provider.

using Microsoft.Extensions.AI;

// As in lesson 01: declare against the INTERFACE so the rest of the file is
// portable. Swap the right-hand side for OpenAI/Ollama and everything below
// keeps working.
IChatClient client = new TypewriterChatClient();

// `Console.Write` (no "Line") prints WITHOUT a trailing newline. That matters
// for streaming -- we want each chunk to appear on the SAME line so the
// answer "grows" in place.
Console.Write("assistant > ");

// `await foreach (var x in src) { ... }` is the asynchronous sibling of plain
// `foreach`. On each iteration, the runtime `await`s the NEXT element of an
// `IAsyncEnumerable<T>`. The thread stays free between elements -- nothing
// is blocked while we wait for the next token.
//
// The collection expression `[ new(ChatRole.User, "...") ]` (C# 12) is a
// shorthand: the compiler infers the target type (IEnumerable<ChatMessage>
// here) and builds the collection. `new(ChatRole.User, "...")` is again the
// target-typed `new` from lesson 01 -- the element type ChatMessage is
// inferred from the surrounding collection expression.
await foreach (var update in client.GetStreamingResponseAsync(
                   [ new(ChatRole.User, "Stream me a poem.") ]))
{
    // Each `update` is a ChatResponseUpdate. `.Text` is the slice of text
    // this particular chunk added. Real providers send a few characters or
    // a single token at a time; our toy client sends a word at a time so
    // the effect is visible to a human reader.
    Console.Write(update.Text);
}

// Drop a newline after the loop so the next shell prompt isn't glued to the
// end of the response.
Console.WriteLine();


// --- Toy streaming client ---------------------------------------------------
//
// Real implementations call the provider's streaming HTTP endpoint (OpenAI's
// `stream: true`, Ollama's chunked HTTP, etc.) and `yield return` each parsed
// chunk to the consumer. We fake the network by yielding one canned word at a
// time with `Task.Delay`, which makes the typewriter effect visible to a human.
//
// A note on what `sealed` and `internal` mean is in lesson 01 (search
// "internal sealed"); not repeated here.
internal sealed class TypewriterChatClient : IChatClient
{
    // `static readonly` = a single shared instance, initialised once, never
    // reassigned. The closest Java analogue is `private static final`.
    //
    // `string[] = [ ... ]` is the collection-expression form again (C# 12).
    // The target type `string[]` is taken from the field declaration, so
    // the compiler builds a plain string array. Equivalent to the older
    // `new string[] { "Roses", "are", ... }`.
    private static readonly string[] _words =
        ["Roses", "are", "red,", "violets", "are", "blue,", "M.E.AI", "streams", "responses", "too."];

    // The non-streaming half of the interface still has to compile. We don't
    // implement it in this lesson -- callers should reach for the streaming
    // method or use the `ToChatResponseAsync()` extension to drain our stream.
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("See lesson 01 for non-streaming.");

    // ITERATOR METHOD ("async iterator", since it combines `async` with
    // `yield return`). An `async IAsyncEnumerable<T>` method is compiled into
    // a state machine: each `yield return x;` hands `x` to the consumer and
    // pauses; the next `await foreach` step resumes the method until the next
    // `yield`. Java has no native equivalent -- the closest you get is
    // Reactor's `Flux.create(sink -> { ... sink.next(x); })`.
    //
    // The attribute `[EnumeratorCancellation]` on `cancellationToken` is the
    // glue that makes the consumer's cancellation token flow into THIS
    // method's `cancellationToken` parameter. Without it, async iterators
    // silently ignore caller cancellation. The fully-qualified name
    // (`System.Runtime.CompilerServices.EnumeratorCancellation`) avoids a
    // `using` directive just for one attribute.
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        foreach (var word in _words)
        {
            // `Task.Delay` is the async sleep -- it returns a Task that
            // completes after the given milliseconds. Awaiting it suspends
            // this method without blocking a thread (unlike `Thread.Sleep`).
            // Passing the cancellation token means a caller stopping the
            // enumeration can cancel the delay too.
            await Task.Delay(120, cancellationToken);

            // `yield return` hands a single value to the consumer's
            // `await foreach` body, then suspends until the consumer asks
            // for the next element. The ROLE field is only meaningful on the
            // first update; subsequent updates can leave it null and the
            // consumer's aggregator (`ToChatResponseAsync`) merges them.
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
        }
    }

    // `GetService` explanation: see lesson 01. Same boilerplate -- we only
    // claim to be an IChatClient when asked, with no keyed registration.
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
