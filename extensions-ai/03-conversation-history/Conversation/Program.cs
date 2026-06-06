// Lesson 03: Multi-turn conversation
// ============================================================================
//
// WHY THIS LESSON EXISTS
// ----------------------------------------------------------------------------
// The single most-common mistake newcomers make with LLMs is treating them as
// if they had memory. They do not. An LLM API call is a PURE FUNCTION:
//
//     (messages, options)  --->  one new assistant message
//
// Nothing about the model changes between calls. There is no server-side
// session, no "I remember our conversation". If you want a chatbot that
// recalls earlier turns, YOUR code must keep the message list and re-send
// the WHOLE history on every call.
//
// (Some hosted products -- e.g. ChatGPT's "thread" API -- DO keep history on
// the server. Under the hood they're doing exactly what we do here: storing
// a list of messages and re-sending it. The raw IChatClient contract is the
// stateless primitive that wrappers like that are built on.)
//
//
// THE CONVERSATION LOOP
// ----------------------------------------------------------------------------
//   1. Build an initial message list. Usually starts with a SYSTEM message
//      that sets the model's standing instructions ("you are a tutor; answer
//      briefly"). Roles are recap-ed in lesson 01.
//   2. Append the next USER turn to the list.
//   3. Pass the WHOLE list to `client.GetResponseAsync(history)`.
//   4. Append `response.Messages` (one OR more assistant messages -- a
//      tool-calling response can produce several) back onto the list.
//   5. Go to step 2.
//
// The list grows without bound, and so does the token cost of each call.
// Real apps eventually need to TRUNCATE or SUMMARISE old turns once the
// total token count nears the model's context-window limit. M.E.AI ships
// reducers in:
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatReducer
// for that; out of scope for basics.
//
//
// THE "QUICK PROMPT" SHORTCUT
// ----------------------------------------------------------------------------
// There is a convenience extension `client.GetResponseAsync("just a string")`
// that wraps the string in a one-message list and calls the real overload.
// It's handy for one-shot tests, but USELESS for real chat -- every call
// starts from a blank list, so the model forgets everything between turns.
// Mention it because beginners reach for it and then wonder why "the model
// has amnesia".
//
//
// WHY A FAKE CLIENT?
// ----------------------------------------------------------------------------
// `MemoryEchoClient` below "uses" the history in a trivial way: it counts
// how many user turns are present and replies with that number. That's
// enough to prove the history is being passed through correctly -- when
// you run this program you'll see "user turn #1", "#2", "#3", confirming
// that each call sees the GROWING list. A real LLM would actually condition
// its reply on the prior turns; the SHAPE of the calling code is the same.

using Microsoft.Extensions.AI;

// Interface-typed variable so the consumer code is provider-agnostic.
IChatClient client = new MemoryEchoClient();

// `var` infers `List<ChatMessage>` from the right-hand side (see lesson 01
// for `var`'s semantics). `new List<ChatMessage> { ... }` uses a COLLECTION
// INITIALIZER -- sugar for constructing the list and `.Add`-ing each element
// inside the braces. Roughly Java's `List.of(...)` except the result is
// MUTABLE (because we'll append to it in the loop below).
var history = new List<ChatMessage>
{
    new(ChatRole.System, "You repeat back how many turns we've had so far."),
};

// `string[] turns = [...]` -- collection expression (C# 12) for an array.
// Equivalent long form: `new string[] { "Hi.", "How many ...", "And now?" }`.
string[] turns = ["Hi.", "How many times have I asked?", "And now?"];

foreach (var userTurn in turns)
{
    // 1. Mutate the SAME list by appending the user's turn. The next call to
    //    GetResponseAsync will see this addition plus everything from prior
    //    iterations -- that's why the count grows.
    history.Add(new ChatMessage(ChatRole.User, userTurn));

    // 2. Send the WHOLE history. Each call ships the entire transcript.
    //    Yes, that means token cost scales with the conversation's length;
    //    that's the trade-off for the stateless API.
    var response = await client.GetResponseAsync(history);

    // 3. Append EVERY message the response produced -- usually one assistant
    //    message, but a function-calling response (lesson 07) can emit a
    //    sequence (tool call + tool result + final text). `AddRange` is the
    //    `List<T>` equivalent of Java's `addAll(Collection)`.
    history.AddRange(response.Messages);

    Console.WriteLine($"user      > {userTurn}");
    Console.WriteLine($"assistant > {response.Text}");
    Console.WriteLine();
}

// Count walkthrough for the run you're about to see:
//   1 system + 3 user + 3 assistant = 7 messages.
// If we hadn't appended response.Messages, the assistant would never see its
// own prior turns and would have no way to maintain a persona across turns.
Console.WriteLine($"final history has {history.Count} messages.");


// --- Toy client that reads the history to compute its reply ----------------
//
// Real providers DO read every message in the list to compute the next one --
// that's literally what an LLM does. Our fake just demonstrates that the list
// is being threaded correctly by counting role==User messages.
internal sealed class MemoryEchoClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `Count(predicate)` is LINQ for "how many elements match this lambda?".
        // `m => m.Role == ChatRole.User` is the predicate -- same lambda shape
        // as Java. Equivalent Java Streams: `messages.stream().filter(m ->
        // m.role == ChatRole.User).count()`.
        int userTurns = messages.Count(m => m.Role == ChatRole.User);

        var reply = $"This is user turn #{userTurns}.";

        // `Task.FromResult(x)` wraps an already-known value in a completed Task,
        // the cheap way to satisfy an `async` signature from synchronous code
        // (see lesson 01 for the longer explanation).
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("See lesson 02 for streaming.");

    // See lesson 01 for the `GetService` pattern -- pipelines use it to walk
    // the chain of wrapped clients and find ones that implement a given type.
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
