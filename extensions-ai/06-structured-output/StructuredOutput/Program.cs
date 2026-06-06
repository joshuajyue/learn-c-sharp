// Lesson 06: Structured output (typed responses)
// ============================================================================
//
// WHERE WE LEFT OFF
// ----------------------------------------------------------------------------
// Lesson 05 ended with us PARSING JSON the model produced. We had to:
//
//   1. Describe the JSON shape in English in the prompt.
//   2. Hope the model honoured it.
//   3. Parse the resulting string by hand with `JsonDocument`.
//   4. Validate every field with an allowlist.
//
// Steps 1, 3 and 4 are mechanical and error-prone. M.E.AI gives you a single
// helper that does all four for you AND uses the provider's STRUCTURED-OUTPUT
// support (when available) so the model is constrained to produce valid JSON
// matching a SCHEMA you derived from a C# type.
//
//
// THE TYPED OVERLOAD
// ----------------------------------------------------------------------------
//   public static Task<ChatResponse<T>> GetResponseAsync<T>(
//       this IChatClient client,
//       IEnumerable<ChatMessage> messages,
//       ChatOptions? options = null,
//       ...);
//
// It's an EXTENSION METHOD on IChatClient -- you call it as if it were an
// instance method (`client.GetResponseAsync<T>(...)`) and the compiler
// dispatches to `ChatClientExtensions`. Java has no equivalent (closest is
// Kotlin's extension functions). The `T` is your DTO type; the return is
// `ChatResponse<T>` -- a `ChatResponse` plus a typed `.Result` of type `T`.
//
// Under the hood it:
//   * Generates a JSON schema from `T` via `AIJsonUtilities` (which uses
//     `System.Text.Json`'s metadata + any `[Description]` attributes you put
//     on properties).
//   * Sets `ChatOptions.ResponseFormat` to a `ChatResponseFormatJson` that
//     carries the schema.
//   * Adds a hidden system message reminding the model what shape to produce.
//   * Deserializes the JSON the model returns into `T` for you.
//
// Modern providers (OpenAI gpt-4o, Azure OpenAI, recent Ollama models) take
// the schema seriously and CONSTRAIN sampling so the model can ONLY emit
// valid JSON matching it. The marketing term is "structured outputs" or
// "JSON mode + schema". Older / smaller models may still drift; the
// `.TryGetResult` shape below lets you handle that gracefully.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatClientExtensions.cs
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/Utilities/AIJsonUtilities.cs
//
//
// MENTAL MODEL FOR `[Description]`
// ----------------------------------------------------------------------------
// `[Description("...")]` (from `System.ComponentModel`) is just an attribute
// the schema generator reads. Each description ends up next to the property
// in the JSON schema sent to the model -- so a `Score` field with description
// "Score from 1 (worst) to 5 (best)" produces much better answers than a
// bare `Score`. Treat these as a PROMPT for individual fields.
//
//
// WHY A FAKE MODEL?
// ----------------------------------------------------------------------------
// `RestaurantReviewModel` below ignores the prompt entirely and always
// returns a canned `ReviewSummary` serialised to JSON. That's enough to
// prove the typed pipeline works end-to-end. With a real provider you'd
// see the same C# code; the model would actually read the review text and
// fill in the fields itself.

// `System.ComponentModel` is the BCL namespace containing `DescriptionAttribute`.
// Boring name; widely used by editors / serialisers / schema tools.
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

IChatClient client = new RestaurantReviewModel();

var prompt = "Review: The pizza was great but the service was painfully slow.";

// Ask for a TYPED response. `T = ReviewSummary`. The framework synthesises a
// JSON schema from `ReviewSummary`, asks the model for that shape, and
// parses for you. The variable type is `ChatResponse<ReviewSummary>` -- a
// regular ChatResponse plus a `.Result` / `.TryGetResult(out T)` for the
// deserialised payload.
//
// Worth noting: `client.GetResponseAsync<T>(...)` is the EXTENSION-METHOD
// dispatch. We didn't have to declare a new interface method; the compiler
// is doing `ChatClientExtensions.GetResponseAsync<ReviewSummary>(client, ...)`.
ChatResponse<ReviewSummary> response = await client.GetResponseAsync<ReviewSummary>(
    [ new(ChatRole.User, prompt) ]);

// `.Result` throws `InvalidOperationException` if the model produced JSON
// that couldn't be deserialised into `T`. `.TryGetResult(out T result)` is
// the non-throwing variant -- returns bool, gives you the value via `out`.
// `out var summary` declares the variable INLINE: equivalent to writing
// `ReviewSummary summary;` on the line above. Java has no equivalent
// (you'd return an `Optional<T>` or a small wrapper record).
if (response.TryGetResult(out var summary))
{
    Console.WriteLine($"sentiment: {summary.Sentiment}");
    Console.WriteLine($"score    : {summary.Score}/5");
    // `string.Join(", ", collection)` -- joins with separator. Java equivalent
    // is `String.join(", ", list)`.
    Console.WriteLine($"topics   : {string.Join(", ", summary.Topics)}");
    Console.WriteLine($"summary  : {summary.OneLineSummary}");
}
else
{
    Console.WriteLine("model returned invalid JSON");
}


// --- The DTO --------------------------------------------------------------
//
// Each `[Description]` is read by the schema generator and embedded in the
// JSON schema sent to the model. They're effectively FIELD-LEVEL PROMPTS:
// good descriptions yield dramatically better fills.
//
// Why properties with public setters instead of a record? `GetResponseAsync<T>`
// uses System.Text.Json to deserialise into `T`; the default policy reaches
// for a parameterless constructor + writable properties. Records would also
// work but you'd need to import the right S.T.J behaviour. Mutable POCOs
// keep the lesson focused.
//
// `public ... = ""` and `public ... = []` are PROPERTY INITIALISERS (C# 6+).
// They give each instance a sensible default so the type is non-null on
// construction even before deserialisation runs. The `[]` is a collection
// expression -- an empty `List<string>` here.
public sealed class ReviewSummary
{
    [Description("Overall sentiment: Positive, Negative, or Mixed")]
    public string Sentiment { get; set; } = "";

    [Description("Score from 1 (worst) to 5 (best)")]
    public int Score { get; set; }

    [Description("Up to 3 short topic tags, e.g. 'food', 'service', 'price'")]
    public List<string> Topics { get; set; } = [];

    [Description("Single-sentence summary, max 120 chars")]
    public string OneLineSummary { get; set; } = "";
}


// --- Toy model that simulates a real provider in JSON-schema mode ---------
//
// A real provider would consult `options.ResponseFormat` (which the typed
// overload set for us) and constrain its sampling to the schema. We just
// emit JSON that matches the type. The shape of YOUR code at the top of
// this file is identical against either implementation.
internal sealed class RestaurantReviewModel : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // The typed overload set `options.ResponseFormat` to a
        // `ChatResponseFormatJson` whose `.Schema` is the JSON schema
        // generated from ReviewSummary. Uncomment to see it printed:
        // Console.WriteLine(((ChatResponseFormatJson)options!.ResponseFormat!).Schema);

        var summary = new ReviewSummary
        {
            Sentiment      = "Mixed",
            Score          = 3,
            Topics         = ["food", "service"],
            OneLineSummary = "Good food held back by slow service.",
        };

        // `JsonSerializer.Serialize(obj)` produces JSON text using
        // System.Text.Json. Java analogue: `objectMapper.writeValueAsString(obj)`
        // (Jackson) or `gson.toJson(obj)` (Gson).
        var json = JsonSerializer.Serialize(summary);

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
