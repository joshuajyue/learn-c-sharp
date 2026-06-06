// Lesson 15: Multi-modal input (vision) + caching
// ============================================================================
//
// WHAT "MULTI-MODAL" MEANS
// ----------------------------------------------------------------------------
// So far every lesson has been TEXT IN, TEXT OUT. Modern frontier models
// accept many more modalities -- images, audio, video, PDFs, even
// spreadsheets -- and the same `IChatClient` interface gracefully extends to
// all of them. The most common non-text modality in 2025+ is VISION ("ask
// the model about a picture"):
//
//     "What's wrong with this part?"   + photo of an industrial widget
//     "Extract the line items"         + photo of a receipt
//     "Describe this UI"               + screenshot
//     "Read the meter"                 + photo of an electricity meter
//
//
// HOW VISION SLOTS INTO IChatClient
// ----------------------------------------------------------------------------
// A `ChatMessage` has not only `.Text` but also `.Contents`, which is a
// list of `AIContent` objects. `TextContent` is just ONE variant of
// `AIContent`. Others include:
//
//     TextContent   -- the prose you've been sending all along
//     DataContent   -- raw bytes + media type (image/png, application/pdf, ...)
//     UriContent    -- HTTP URL + media type (for "fetch this and look at it")
//     FunctionCallContent / FunctionResultContent  -- the tool-call types
//                                                    from lesson 07
//
// To send an image you build a message with mixed contents:
//
//     new ChatMessage(ChatRole.User,
//     [
//         new TextContent("What's in this image?"),
//         new DataContent(File.ReadAllBytes("widget.png"), "image/png"),
//     ]);
//
// The provider serialises the data alongside the text (OpenAI uses base64;
// Azure uses an inline part; Anthropic has its own format) and hands the
// whole thing to the model. Your code stays at the abstraction layer.
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/DataContent.cs
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/UriContent.cs
//
//
// THREE PATTERNS THAT PAIR NATURALLY WITH VISION
// ----------------------------------------------------------------------------
//   * STRUCTURED OUTPUT (lesson 06).
//       Vision is most useful when you parse the model's answer into typed
//       fields ("status: OK | DEFECT, defectCount: int, severity: ..."). The
//       `GetResponseAsync<T>` helper closes the loop in ~20 lines of code:
//       photo in, typed inspection record out.
//
//   * CACHING.
//       Vision and "reasoning" models are EXPENSIVE -- often 5-50x the
//       per-token cost of text-only models. If your app re-asks the same
//       question about the same image (very common: thumbnail previews,
//       retries, A/B tests), you should serve those out of a cache instead
//       of paying for the same call twice. M.E.AI ships
//       `ChatClientBuilder.UseDistributedCache(IDistributedCache)` (lesson 08
//       covered builder middleware in general); the cache hashes the
//       MESSAGES + OPTIONS to form a key, so byte-identical image bytes hit
//       the same entry.
//
//   * SMALL MODELS.
//       Vision models come in tiers. A small / cheap one (gpt-4o-mini,
//       llama-3.2-vision) is plenty for routine classification; reserve the
//       big model for AMBIGUOUS cases. The triage pattern -- cheap model
//       answers, big model adjudicates only when confidence is low -- can
//       save 10x on bills with no quality loss.
//
//
// WHY A FAKE MODEL?
// ----------------------------------------------------------------------------
// `VisionModel` below pretends to be a real vision LLM by reading the FIRST
// BYTE of any attached image and "classifying" it (0xFF = DEFECT, anything
// else = OK). That's enough to prove the multi-modal pipeline works and to
// see the cache hit on repeated calls. With a real provider you'd swap
// `VisionModel` for an `OpenAIClient(...).AsIChatClient()` and the consumer
// code above doesn't change.

// System.Security.Cryptography: BCL crypto. We only use SHA256.HashData
// here, as a deterministic ID for our pretend images so the log lines tag
// each image with a short hex hash.
using System.Security.Cryptography;
using Microsoft.Extensions.AI;
// IDistributedCache is the BCL abstraction for a key-value cache that COULD
// live on another machine (Redis, SQL, Azure Cache for Redis...). For demos
// you use the in-memory implementation below.
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
// `Options.Create(...)` (in M.E.Options) wraps a plain options instance in
// an `IOptions<T>`, which is what most config-driven types want.
using Microsoft.Extensions.Options;

// In-memory IDistributedCache. Real apps register Redis or SQL with their
// own `AddStackExchangeRedisCache(...)` / `AddDistributedSqlServerCache(...)`
// call; for the lesson, MemoryDistributedCache keeps everything in RAM.
IDistributedCache cache = new MemoryDistributedCache(
    Options.Create(new MemoryDistributedCacheOptions()));

// PIPELINE (outer -> inner):
//   client (what you call) -> DistributedCache -> CallCounter -> VisionModel
//
// `ChatClientBuilder.Use(...)` and `UseDistributedCache` add WRAPPERS around
// the inner client. The builder calls each `Use` factory with the
// previously-built inner, so layer ORDER matters -- the LAST `Use` call
// becomes the OUTERMOST wrapper (lesson 08).
//
// We put the CACHE on the outside so cache hits never reach the counter or
// the model. The COUNTER sits between cache and model so we can observe
// EXACTLY how many calls actually pay for inference.
var counter = new CallCounter(new VisionModel());

IChatClient client = new ChatClientBuilder(counter)
    .UseDistributedCache(cache)
    .Build();

// --- Pretend "images" -- 4-byte arrays. The model ignores most of the
// bytes; the cache hashes them, so the SECOND call with image1 must hit
// the cache and the call to image2 must miss.
//
// `byte[] image1 = [0x01, ...]` is the collection-expression form for an
// implicit byte array. Equivalent: `new byte[] { 0x01, ... }`. `0xFF` is a
// hex literal for 255 (same as Java/C).
byte[] image1 = [0x01, 0x02, 0x03, 0x04];
byte[] image2 = [0xFF, 0xEE, 0xDD, 0xCC];

await Inspect(image1, "first image, first time");
await Inspect(image1, "first image, again (expect cache HIT)");
await Inspect(image2, "second image (expect cache MISS)");

Console.WriteLine($"\ninner model was called {counter.Count} time(s) -- the rest were cache hits.");


// LOCAL FUNCTION (lesson 04) that captures the `client` variable. Used
// just to keep the body of the three calls short and readable.
async Task Inspect(byte[] imageBytes, string label)
{
    // A MULTI-MODAL user message: TWO contents in the same message --
    // a text prompt AND a chunk of image bytes. The model sees both in the
    // SAME turn, so "this prompt is about THIS image" is implicit.
    //
    // `new DataContent(bytes, mediaType)` is the inline-bytes form;
    // `new UriContent(uri, mediaType)` is the URL form. Providers convert
    // each to their wire format (typically base64 for inline bytes).
    var message = new ChatMessage(ChatRole.User,
    [
        new TextContent("Classify this widget photo."),
        new DataContent(imageBytes, "image/png"),
    ]);

    var response = await client.GetResponseAsync([message]);
    Console.WriteLine($"[{label}]  hash={Hash(imageBytes)}  -> {response.Text}");
}

// `Convert.ToHexString(bytes)` is the BCL one-liner for "bytes to hex
// string" (added in .NET 5). `[..8]` is the range-expression substring:
// first 8 hex chars, plenty for distinguishing a couple of toy images.
// `SHA256.HashData` is the no-allocation static API (vs. instantiating a
// SHA256 object). Java analogue:
//   MessageDigest.getInstance("SHA-256").digest(bytes) + bytesToHex.
static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes))[..8];


// --- Middleware: a counter the cache sits in front of ----------------------
//
// Using `DelegatingChatClient` (the MEAI base class for middleware) means
// we override ONLY the method we care about; everything else (streaming,
// GetService, Dispose) forwards to the inner client automatically. Same
// pattern as lesson 08's TimingClient / RetryOnceClient.
//
// We don't override the streaming method; the lesson only exercises the
// non-streaming path, and the inner model's streaming throws anyway.
internal sealed class CallCounter(IChatClient inner) : DelegatingChatClient(inner)
{
    // Auto-property with a private setter -- caller can READ Count via the
    // public getter, but only this class can WRITE it.
    public int Count { get; private set; }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `++` is the same as in Java/C. We bump BEFORE forwarding so the
        // count reflects "request reached the model", which is what we
        // care about (cache hits are filtered out before reaching us).
        Count++;
        return base.GetResponseAsync(messages, options, cancellationToken);
    }
}


// --- Toy "vision" model ----------------------------------------------------
//
// A real vision provider would send the image bytes to a hosted model
// (gpt-4o, llama-3.2-vision, claude-3.5-sonnet, ...). Our fake just inspects
// the FIRST BYTE to decide a verdict, which is enough to prove the
// multi-modal pipeline works end-to-end.
internal sealed class VisionModel : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Walk all messages, flatten their Contents lists, filter to ones
        // that ARE DataContent. `SelectMany` is the LINQ flatMap (see
        // lesson 07); `.OfType<T>()` filters + casts in one step.
        // `.FirstOrDefault()` returns the first match or null.
        var data = messages
            .SelectMany(m => m.Contents)
            .OfType<DataContent>()
            .FirstOrDefault();

        // Nested ternary: if no image attached, special-case the verdict;
        // otherwise look at the first byte. `data.Data.Span[0]` -- `Data`
        // is a `ReadOnlyMemory<byte>`, `.Span` is the `ReadOnlySpan<byte>`
        // view, `[0]` is the first byte. `0xFF` = 255.
        string verdict = data is null
            ? "no image attached"
            : data.Data.Span[0] == 0xFF ? "DEFECT detected" : "OK";

        return Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, $"verdict: {verdict}")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
