// Lesson 04: ChatOptions -- shaping the request
// ============================================================================
//
// WHY YOU CARE ABOUT OPTIONS
// ----------------------------------------------------------------------------
// Every call to `GetResponseAsync` takes an optional `ChatOptions` parameter.
// That object carries the KNOBS that real LLM providers all expose -- the
// same knobs you'd otherwise set on a raw HTTP POST body. M.E.AI's job is to
// give them a single common name so your code is portable across providers:
//
//   ModelId             -- which model to use ("gpt-4o-mini", "llama3.2:8b", ...)
//   Temperature         -- 0.0 = deterministic, 1.0+ = random / creative.
//                          Sampling noise added to the next-token distribution.
//   MaxOutputTokens     -- hard cap on response length (in tokens, not chars!)
//   TopP, TopK          -- alternative ways to narrow the candidate token pool
//   FrequencyPenalty,
//   PresencePenalty     -- discourage the model from repeating itself
//   StopSequences       -- strings that, if generated, stop the response early
//   ResponseFormat      -- text (default) or JSON (forces structured output)
//   Seed                -- request reproducibility when the provider supports it
//   Tools               -- callable functions the model may invoke (lesson 07)
//   ToolMode            -- Auto / Required / None
//   AdditionalProperties-- provider-specific extras (escape hatch)
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatOptions.cs
//
//
// THE TWO WAYS TO SUPPLY OPTIONS
// ----------------------------------------------------------------------------
// You can either:
//   * Pass a fresh `ChatOptions` per call (this lesson), OR
//   * Use `ChatClientBuilder.ConfigureOptions(opts => { ... })` to set
//     defaults at pipeline-construction time -- handy when every call in
//     your service wants the same model and temperature. Covered in lesson 08
//     (middleware) and lesson 09 (DI).
//
//
// MENTAL MODEL FOR TEMPERATURE
// ----------------------------------------------------------------------------
// An LLM picks each token by sampling from a probability distribution.
// Temperature scales that distribution:
//
//   T = 0.0  ->  "always pick the most likely token"        (deterministic)
//   T = 0.7  ->  "pick from the most plausible handful"     (balanced)
//   T = 1.2  ->  "weird choices possible; creative writing" (high variance)
//
// For factual tasks (extraction, RAG answers, classification) you want LOW
// temperature. For brainstorming and creative writing, HIGHER. There is no
// "best" value -- it's an application-level decision.
//
//
// MENTAL MODEL FOR JSON / STRUCTURED OUTPUT
// ----------------------------------------------------------------------------
// `ResponseFormat = ChatResponseFormat.Json` tells the provider "the model
// MUST emit valid JSON". OpenAI calls this "JSON mode". You then parse the
// string with `JsonSerializer.Deserialize<T>(...)`. Lesson 06 shows a
// higher-level wrapper (`GetResponseAsync<T>`) that generates a JSON SCHEMA
// from a C# record and parses the result for you in one step.
//
//
// WHY A FAKE CLIENT?
// ----------------------------------------------------------------------------
// `ToyOptionsClient` below READS the options to decide its reply, so when
// you run this you'll see three meaningfully-different outputs from the same
// prompt. A real provider also reacts to these options, just with much more
// nuance. The shape of YOUR code is identical either way.

using Microsoft.Extensions.AI;

IChatClient client = new ToyOptionsClient();

// Three calls with three different option sets. Same prompt every time;
// only the knobs change.
//
// `new ChatOptions { ... }` is an OBJECT INITIALIZER (lesson 01 covered the
// syntax): construct with the default constructor, then set named properties
// inside the braces. Java equivalent would be a builder or a constructor
// with many parameters; C# uses this shape for any type with public setters.
//
// `0.0f` and `0.9f` are FLOAT literals (the `f` suffix is required because
// the default for `0.9` would be `double`, and Temperature is `float?`).
await Ask(new ChatOptions { Temperature = 0.0f, MaxOutputTokens = 50 });
await Ask(new ChatOptions { Temperature = 0.9f, MaxOutputTokens = 50 });
await Ask(new ChatOptions { ResponseFormat = ChatResponseFormat.Json });

// LOCAL FUNCTION: a named method declared inside another method. It captures
// the enclosing `client` variable (same closure semantics as Java lambdas).
// Useful for sharing logic between several `await`-style call sites without
// promoting it to a top-level method.
async Task Ask(ChatOptions opts)
{
    var resp = await client.GetResponseAsync(
        [ new(ChatRole.User, "tell me a fact about cats") ], opts);

    // `,-4` inside `{value,-4}` is FORMAT SPECIFIER syntax: width 4, LEFT
    // aligned (negative). The fmt label uses pattern matching (`is`) to
    // distinguish JSON from text response formats. `ChatResponseFormatJson`
    // is the concrete subclass for JSON; the abstract base `ChatResponseFormat`
    // also has `Text` (the default) and `Json` (a static helper that returns
    // a fresh `ChatResponseFormatJson`).
    Console.WriteLine($"[T={opts.Temperature, -4} fmt={(opts.ResponseFormat is ChatResponseFormatJson ? "json" : "text")}] " +
                      $"{resp.Text}");
}


// --- Toy client that actually reacts to a few options -----------------------
//
// Real providers translate each option into their own wire format (OpenAI's
// JSON body, Ollama's options block, etc.). The point of this file is that
// from the CONSUMER side, you just set the field on ChatOptions and trust
// the provider to honour it.
internal sealed class ToyOptionsClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string reply;

        // PATTERN MATCHING with `is`: tests whether the runtime type of
        // `options.ResponseFormat` is (or derives from) `ChatResponseFormatJson`.
        // `options?.X` -- if `options` is null the whole expression yields
        // null, so the `is` test produces false -- same `?.` short-circuit
        // explained in lesson 01.
        if (options?.ResponseFormat is ChatResponseFormatJson)
        {
            // `"""..."""` is a RAW STRING LITERAL (C# 11). Inside it, you
            // don't have to escape regular double quotes -- handy for JSON.
            // The Java equivalent (text blocks, `"""..."""`) has the same
            // shape but slightly different leading-whitespace rules.
            reply = """{ "subject": "cats", "fact": "cats sleep ~15 hours a day" }""";
        }
        // `?? 0f` -- NULL-COALESCING: if Temperature is null, treat it as 0.
        // Then compare >= 0.7f. Without the `??`, comparing a `float?` to
        // `0.7f` would lift the comparison to nullable and yield null for
        // null inputs (and an `if (null)` is a compile error).
        else if ((options?.Temperature ?? 0f) >= 0.7f)
        {
            reply = "CATS ARE SECRETLY TINY DRAGONS WITH RETRACTABLE CLAWS!!1";
        }
        else
        {
            reply = "Cats sleep about 15 hours a day on average.";
        }

        // PATTERN MATCHING with a `var`-style declaration:
        //   `x is int max`  =>  if x is non-null and an int, bind it to `max`.
        // Equivalent long form would be `if (options?.MaxOutputTokens != null) {
        // int max = options.MaxOutputTokens.Value; if (...) ... }`. The
        // pattern syntax keeps the nullability check and the variable binding
        // in one expression.
        //
        // `reply[..(max * 4)]` is a RANGE EXPRESSION: substring from index 0
        // (the omitted lower bound) up to (but not including) `max * 4`. The
        // `..` is the range operator. Roughly Java's `reply.substring(0, n)`.
        if (options?.MaxOutputTokens is int max && reply.Length > max * 4)
        {
            reply = reply[..(max * 4)];
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("See lesson 02 for streaming.");

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
