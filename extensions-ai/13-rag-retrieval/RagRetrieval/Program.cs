// Lesson 13: RAG, part 2 -- retrieval, prompting, citations
// ============================================================================
//
// PUTTING IT ALL TOGETHER
// ----------------------------------------------------------------------------
// Lesson 12 produced a CORPUS of (chunk, embedding) records and stored them.
// Lesson 10 taught you how to score similarity between two embeddings.
// Lesson 11 taught you how to scale that search. THIS lesson stitches all
// three together into an end-to-end Q&A bot:
//
//     1. EMBED the user's question.
//     2. RETRIEVE the top-K nearest chunks from the index (cosine similarity).
//     3. PROMPT-BUILD: include the chunks as a "context" block with IDs.
//     4. ASK the LLM to answer using ONLY that context, citing chunk IDs.
//     5. DISPLAY the answer along with its citations.
//
// Architecturally that's it. Every "ChatGPT for your docs" / "AI search"
// product you've seen is built on this exact loop; the differences are in
// the quality of the chunker, the choice of vector store, and the rigor of
// the system prompt.
//
//
// TWO DESIGN RULES THAT SHOW UP EVERYWHERE
// ----------------------------------------------------------------------------
//   GROUND THE MODEL.
//     The system prompt must say -- explicitly -- "answer ONLY using the
//     context below; if the answer isn't there, say 'I don't know'."
//     Without that instruction, the model happily fills gaps with its
//     pre-training knowledge ("hallucinates"). The whole reason your user
//     pointed RAG at a private corpus is to get answers from THAT corpus;
//     making things up defeats the point.
//
//   CITE THE SOURCE.
//     Each chunk in the prompt is tagged with a stable ID (the one we built
//     in lesson 12). The model is instructed to include that ID inline
//     (e.g. "[doc#3]") alongside any claim that came from it. The UI
//     renders those IDs as links back to the source. Users can verify;
//     auditors can prove provenance; debugging a wrong answer becomes
//     "which chunks did it cite?" instead of "where did this come from?".
//
// "I DON'T KNOW" IS A FEATURE. A RAG bot that politely refuses on
// unsupported questions is dramatically more useful than one that
// confidently lies -- because the user can act on the refusal (rephrase
// the question, add more docs, give up gracefully). Confident lies erode
// trust in the whole product.
//
//
// WHY FAKES?
// ----------------------------------------------------------------------------
// `TopicEmbedder` is the same trick as lessons 10-12 (deterministic hash +
// topic bias). `GroundedChat` is a fake LLM that ACTUALLY READS the system
// prompt's rules: it scans the context block for keyword hits, returns the
// matching chunk's text with its citation, and refuses with the documented
// phrase when nothing matches. A real LLM does this reasoning itself when
// given the same system prompt; our fake just mechanically enforces it so
// the lesson runs offline.

using System.Text;
using Microsoft.Extensions.AI;

// --- Tiny fake corpus (in real life, this came out of lesson 12 + a vector DB).
// Each chunk has a stable ID (source filename + ordinal) and its text.
var corpus = new[]
{
    new Chunk("cat-care.md#0", "Cats are obligate carnivores and need animal protein. Adult cats eat two meals a day."),
    new Chunk("cat-care.md#1", "Water should always be available. Many cats prefer running water from a pet fountain."),
    new Chunk("cat-care.md#2", "Short-haired cats need weekly brushing; long-haired breeds need daily brushing."),
    new Chunk("cat-care.md#3", "Annual vet checkups are recommended; seniors over ten should see a vet twice a year."),
    new Chunk("dog-care.md#0", "Dogs benefit from daily walks and structured play sessions."),
};

IEmbeddingGenerator<string, Embedding<float>> embedder = new TopicEmbedder(dim: 16);
IChatClient chat = new GroundedChat();

// PRE-EMBED the corpus once. In a production system this happens at ingest
// time (lesson 12) and lives in a vector DB; we keep it in memory here.
// `.ToArray()` materialises the GeneratedEmbeddings sequence so we can
// index into it by position below.
var corpusVecs = (await embedder.GenerateAsync(corpus.Select(c => c.Text))).ToArray();

// Two test questions: one IS in the corpus (senior cat vet visits), and
// one IS NOT (cat potty training). The bot should answer the first with a
// citation and politely refuse the second.
string[] questions =
[
    "How often should a senior cat see the vet?",
    "What's the best way to potty-train a cat?",   // not in corpus -- should answer "don't know"
];

foreach (var question in questions)
{
    // 1. Embed the question with the same generator we used for the corpus.
    //    Using the SAME embedder for both sides is critical: vectors from
    //    two different models live in incompatible spaces and the cosine
    //    similarities are meaningless.
    var queryVec = (await embedder.GenerateAsync([question])).First().Vector.ToArray();

    // 2. Retrieve TOP-2 chunks by cosine similarity. `Select((c, i) => ...)`
    //    -- the LINQ overload that gives you element + index, used here to
    //    pair each corpus chunk with its pre-computed embedding (same index).
    //    Then we project to a tuple, sort by score descending, take the top
    //    K. This is the same "rank by similarity" pattern from lesson 10.
    var topK = corpus
        .Select((c, i) => (chunk: c, score: Cosine(corpusVecs[i].Vector.Span, queryVec)))
        .OrderByDescending(p => p.score)
        .Take(2)
        .ToList();

    // 3. Build the prompt. `StringBuilder` (`System.Text`) is the BCL's
    //    mutable string builder -- same name and role as Java's
    //    `java.lang.StringBuilder`. For >2 concatenations it's faster than
    //    `string +=` because it avoids allocating an intermediate string
    //    each time. `AppendLine` adds the string plus a platform newline.
    var contextBlock = new StringBuilder();
    foreach (var (chunk, _) in topK)
        contextBlock.AppendLine($"[{chunk.Id}] {chunk.Text}");

    // The SYSTEM PROMPT is what makes this RAG instead of plain chat.
    // Three rules: use only the context, refuse with a documented phrase,
    // cite the chunks. Each rule maps to a quality property of the answer.
    var systemPrompt = """
        You are a helpful assistant that answers questions about pet care.
        RULES:
        * Use ONLY the facts in the "context" block below.
        * If the answer is not in the context, reply "I don't know based on the provided sources."
        * After your answer, include the IDs of every chunk you used, in [brackets].
        """;

    // The USER PROMPT is the context block followed by the actual question.
    // Real systems sometimes interleave them differently or move the
    // context into a system message -- experiment, then measure.
    var userPrompt = $"context:\n{contextBlock}\nquestion: {question}";

    var response = await chat.GetResponseAsync(
        [ new(ChatRole.System, systemPrompt), new(ChatRole.User, userPrompt) ]);

    Console.WriteLine($"Q: {question}");
    Console.WriteLine($"top sources considered:");
    foreach (var (c, score) in topK)
    {
        // `c.Text[..Math.Min(60, c.Text.Length)]` -- range expression
        // substring guarded against short texts. Prints a 60-char preview
        // so the rank shows up without dumping the whole chunk.
        Console.WriteLine($"  {score:F3}  [{c.Id}] {c.Text[..Math.Min(60, c.Text.Length)]}...");
    }
    Console.WriteLine($"A: {response.Text}");
    Console.WriteLine();
}


// Cosine again -- see lessons 10/11 for the longer explanation. Range [-1, 1],
// higher = more similar. The `1e-9f` is the divide-by-zero safety net.
static float Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
{
    float dot = 0, ma = 0, mb = 0;
    for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; ma += a[i] * a[i]; mb += b[i] * b[i]; }
    return dot / (MathF.Sqrt(ma) * MathF.Sqrt(mb) + 1e-9f);
}


// Same `record` pattern as earlier lessons -- immutable data carrier with
// value equality and a generated ToString. We treat chunks as plain data.
public record Chunk(string Id, string Text);


// --- Fake LLM that actually obeys the grounding instructions --------------
//
// A real LLM, given the same system prompt, would read the context and
// generate a grounded answer. Our fake mimics that with deterministic logic:
//
//   1. Parse out the question from the user message.
//   2. Parse out every `[chunk-id] text` line from the context block.
//   3. Pick the first chunk whose text contains a meaningful (length>3)
//      word from the question.
//   4. If found, return that chunk's text + citation.
//   5. Otherwise, return the documented refusal phrase.
//
// This is brittle by design -- it's a teaching stand-in. Real LLMs do
// MUCH better. The point is to show the SHAPE of the RAG loop.
internal sealed class GroundedChat : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `messages.Last(predicate).Text!` -- the LINQ `Last` throws if the
        // sequence has no matching element; `!` is the null-forgiving op
        // (lesson 05). Both are appropriate here because we know the user
        // message is present and non-null.
        var userText = messages.Last(m => m.Role == ChatRole.User).Text!;

        // Extract the question after "question:". `IndexOf` returns the
        // position (or -1 if absent); `+9` skips past the literal
        // "question:". `[..(...)]` is the range expression: "from this
        // index to the end". `Trim().ToLowerInvariant()` normalises for
        // case-insensitive word matching below.
        var question = userText[(userText.IndexOf("question:") + 9)..].Trim().ToLowerInvariant();

        // Parse the context block. Lines that start with `[` and contain `]`
        // are our `[id] text` rows.
        var chunks = new List<(string id, string text)>();
        foreach (var line in userText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.Contains(']'))
            {
                var end = trimmed.IndexOf(']');
                // `trimmed[1..end]` = id between the brackets.
                // `trimmed[(end + 2)..]` = text after "] ".
                chunks.Add((trimmed[1..end], trimmed[(end + 2)..]));
            }
        }

        // Find the first chunk whose text contains any meaningful word from
        // the question. `Any(word => ...)` returns true if any element
        // matches; chained with `FirstOrDefault` it scans chunks and picks
        // the first hit. `default` for a (string, string) tuple is
        // (null, null), which we compare against below.
        var hit = chunks.FirstOrDefault(c =>
            question.Split(' ').Any(word =>
                word.Length > 3 && c.text.Contains(word, StringComparison.OrdinalIgnoreCase)));

        // The TERNARY picks the refusal or the answer. `hit == default`
        // compares against the default tuple (both fields null).
        string reply = hit == default
            ? "I don't know based on the provided sources."
            : $"{hit.text.TrimEnd('.')}. [{hit.id}]";

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}


// --- Topic-biased embedder (same pattern as lessons 10-12) ----------------
//
// Slot 0..5 are reserved for topic dims; the rest are hash-spread so unrelated
// words don't collide with topic dimensions. Real production swaps this for
// an actual embedding model.
internal sealed class TopicEmbedder(int dim) : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly Dictionary<string, int> _topic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cat"]=0,["cats"]=0,["kitten"]=0,["feline"]=0,
        ["vet"]=1,["veterinary"]=1,["senior"]=1,["checkup"]=1,["annual"]=1,
        ["water"]=2,["fountain"]=2,["drink"]=2,["meal"]=2,["food"]=2,["eat"]=2,["carnivore"]=2,["protein"]=2,
        ["brush"]=3,["brushing"]=3,["groom"]=3,["grooming"]=3,["short-haired"]=3,["long-haired"]=3,
        ["dog"]=4,["dogs"]=4,["walk"]=4,["walks"]=4,["play"]=4,
        ["potty"]=5,["train"]=5,["training"]=5,["litter"]=5,
    };

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var text in values)
        {
            var v = new float[dim];
            foreach (var raw in text.Split([' ', '.', ',', '?', '!', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var w = raw.ToLowerInvariant();
                int slot = _topic.TryGetValue(w, out var t) ? t : (Math.Abs(w.GetHashCode()) % (dim - 6)) + 6;
                v[slot] += 1f;
            }
            list.Add(new Embedding<float>(v));
        }
        return Task.FromResult(list);
    }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
