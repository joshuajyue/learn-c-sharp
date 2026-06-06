// Lesson 10: Embeddings and semantic search
// ============================================================================
//
// WHAT IS AN EMBEDDING?
// ----------------------------------------------------------------------------
// An EMBEDDING is a fixed-length vector of floating-point numbers (commonly
// 384, 768, 1024, or 1536 dimensions depending on the model) that represents
// the MEANING of a piece of text. The crucial property is geometric:
//
//     texts with similar meaning   --->   vectors that point similar directions
//     texts with unrelated meaning --->   vectors that point different directions
//
// Distance (or angle) in that high-dimensional space approximates SEMANTIC
// SIMILARITY. "king" and "queen" land near each other. "king" and
// "spaghetti" land far apart. The model that produces the vectors -- the
// EMBEDDING MODEL -- has been trained on huge text corpora until that
// property emerges. You don't have to know how it works; you just need to
// trust the math.
//
//
// WHY YOU CARE
// ----------------------------------------------------------------------------
// Embeddings are the basic building block for:
//
//   * SEMANTIC SEARCH -- find documents matching a query by MEANING, not
//     keyword overlap. ("how do I speed up my queries" matches "use an
//     index" even though they share no words.)
//   * RAG (Retrieval-Augmented Generation, lessons 12 & 13) -- pull the
//     most relevant documents and hand them to the LLM as context.
//   * CLUSTERING / topic discovery -- group similar items.
//   * DEDUPLICATION -- "is this question already in our FAQ?".
//   * CLASSIFICATION -- nearest-class wins.
//
// They're cheap (a few cents per million tokens with real providers) and
// fast (orders of magnitude cheaper than calling a chat model).
//
//
// THE ABSTRACTION
// ----------------------------------------------------------------------------
//   public interface IEmbeddingGenerator<TInput, TEmbedding> : IDisposable
//       where TEmbedding : Embedding
//   {
//       Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
//           IEnumerable<TInput> values, EmbeddingGenerationOptions?, CancellationToken);
//       object? GetService(Type, object?);
//   }
//
// Source: dotnet/extensions
//   src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/IEmbeddingGenerator.cs
//
// Type parameter walkthrough:
//   `TInput`     -- almost always `string` in practice.
//   `TEmbedding` -- almost always `Embedding<float>` (vector of 32-bit floats).
//
// The shape mirrors `IChatClient`: one abstraction, many provider
// implementations (OpenAI's text-embedding-3-small/large, Azure OpenAI,
// Ollama's nomic-embed-text, ...). You write code against the interface,
// you swap the implementation at registration time.
//
// `where TEmbedding : Embedding` is a GENERIC CONSTRAINT -- the type
// parameter must derive from `Embedding`. Java analogue: `<T extends Embedding>`.
//
//
// COSINE SIMILARITY -- THE STANDARD METRIC
// ----------------------------------------------------------------------------
// Given two vectors A and B, COSINE SIMILARITY is
//
//     cos(theta) = dot(A, B) / (||A|| * ||B||)
//
// Range: [-1, 1]. 1 = pointing the same direction (most similar); 0 =
// orthogonal (unrelated); -1 = opposite. For embeddings produced by modern
// models, scores typically sit in the [0, 1] range -- and the EXACT score
// isn't meaningful, only the RANKING of one document against another.
// "Document A has cosine 0.83 with the query" by itself tells you nothing;
// "A is more similar to the query than B is" is what you act on.
//
//
// WHY A FAKE GENERATOR?
// ----------------------------------------------------------------------------
// Real embedding models are trained so semantic similarity emerges. To run
// this lesson offline we cheat: `HashEmbeddingGenerator` projects each word
// to a fixed slot in the vector (with manual TOPIC BIAS for a handful of
// keywords so the ranking actually makes sense). It is NOT semantic in any
// real sense; it just produces a deterministic, repeatable demo. The
// CONSUMER code -- the `GenerateAsync` call and the ranking with cosine --
// is identical against a real provider.

using Microsoft.Extensions.AI;

// Interface-typed variable so swapping in a real generator is one line.
IEmbeddingGenerator<string, Embedding<float>> embedder = new HashEmbeddingGenerator(dimensions: 32);

// Collection expression `[ ... ]` (C# 12) for a `string[]`. Target type is
// taken from the declaration. Equivalent older form:
// `string[] documents = new[] { ... };`
string[] documents =
[
    "Cats love to nap in sunny spots.",
    "Dogs enjoy playing fetch in the park.",
    "Database indexes speed up SELECT queries.",
    "SQL joins combine rows from multiple tables.",
];

// One BATCHED call -- pass the whole list and get a `GeneratedEmbeddings`
// back. Real providers charge per call AND per token, so batching is
// dramatically cheaper than calling once per document.
var docEmbeddings = await embedder.GenerateAsync(documents);

// `(await embedder.GenerateAsync([query])).First()` -- await yields a
// `GeneratedEmbeddings<Embedding<float>>` (which is `IList<Embedding<float>>`),
// `.First()` is the LINQ "first element" (throws if empty). We pass a
// one-element collection because the generator is batched-first.
string query = "How can I make my queries faster?";
var queryEmbedding = (await embedder.GenerateAsync([query])).First();

// Rank every document by cosine similarity to the query.
//
// `Zip(a, b, (x, y) => ...)` walks two sequences in lockstep and projects
// each pair through the selector. Java Streams' equivalent requires zipping
// manually with `IntStream.range`; Kotlin has the same `zip` shape.
//
// `(text, score: ...)` is a TUPLE LITERAL with a NAMED field: the result
// has an `.text` and a `.score`. C# tuples are lightweight value types; no
// allocation if they're consumed inline.
//
// `.Vector.Span` -- the Embedding stores its data as `ReadOnlyMemory<float>`,
// and `.Span` gives you a `ReadOnlySpan<float>` (a stack-only view that lets
// the JIT optimise tight loops on it, with no bounds-check on each access).
// Spans are roughly "fat pointer over array" -- closer to C's `(ptr, len)`
// than to anything in Java.
var ranked = documents
    .Zip(docEmbeddings, (text, emb) => (text, score: CosineSimilarity(queryEmbedding.Vector.Span, emb.Vector.Span)))
    .OrderByDescending(r => r.score)
    .ToList();

Console.WriteLine($"query: {query}");
Console.WriteLine();

// `foreach (var (text, score) in ranked)` -- TUPLE DECONSTRUCTION in the
// loop variable. Equivalent to `var r = ...; var text = r.text; var score = r.score;`.
foreach (var (text, score) in ranked)
{
    // `{score, 6:F3}` is a COMPOSITE FORMAT: width 6 right-aligned,
    // formatted as fixed-point with 3 decimals. Equivalent printf: `%6.3f`.
    Console.WriteLine($"  {score, 6:F3}  {text}");
}


// `ReadOnlySpan<float>` parameters mean "read-only window over a sequence
// of floats; doesn't allocate; can't outlive the call". Tight numeric loops
// are the textbook span use case. Java has no direct equivalent -- the
// closest is `FloatBuffer`, which is much heavier.
//
// `static` on a local function: see lesson 05 (forbids closures, makes it
// a pure helper).
static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
{
    // Cosine similarity = dot(A, B) / (||A|| * ||B||).
    // Range [-1, 1]: 1 = identical direction, 0 = unrelated, -1 = opposite.
    //
    // Accumulating dot product AND both magnitudes in one pass means we
    // only walk the vectors once -- worth it when D is 1536+. `MathF` is
    // the float-specialised math class (vs `Math.Sqrt` which takes/returns
    // double). The `1e-9f` guard prevents division-by-zero for all-zero
    // vectors (which our fake can produce for empty strings).
    float dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot  += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB) + 1e-9f);
}


// --- Toy embedding generator ------------------------------------------------
//
// Real embedding models are huge neural networks trained to produce
// vectors where similar texts land near each other. We do not have one of
// those handy in an offline lesson. So we cheat: we project each WORD into
// a deterministic slot of a 32-dim vector, with a hand-picked TOPIC BIAS
// that pushes "SQL" / "query" / "database" words into one slot, "cat"
// words into another, etc.
//
// The output is NOT semantically meaningful in any general sense -- e.g.
// "feline" and "cat" wouldn't be related here unless I hard-coded that.
// It IS deterministic, fast, and produces the topic-correct ranking you'll
// see in the demo, so the lesson runs and the math is real.
internal sealed class HashEmbeddingGenerator(int dimensions) : IEmbeddingGenerator<string, Embedding<float>>
{
    // `new(StringComparer.OrdinalIgnoreCase)` -- the dictionary is keyed by
    // string with case-insensitive ordinal comparison. The target type
    // `Dictionary<string, int>` is inferred from the field declaration, so
    // the constructor name is omitted (target-typed `new`, lesson 01).
    private static readonly Dictionary<string, int> _topicBias = new(StringComparer.OrdinalIgnoreCase)
    {
        // Slot 0 = "database-y" topic. Any word in this group pumps slot 0.
        ["sql"] = 0, ["query"] = 0, ["queries"] = 0, ["database"] = 0, ["join"] = 0, ["joins"] = 0, ["index"] = 0, ["indexes"] = 0, ["select"] = 0, ["faster"] = 0,
        // Slot 1 = "cats".
        ["cat"] = 1, ["cats"] = 1, ["nap"] = 1, ["sunny"] = 1,
        // Slot 2 = "dogs".
        ["dog"] = 2, ["dogs"] = 2, ["fetch"] = 2, ["park"] = 2,
    };

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // `GeneratedEmbeddings<T>` is the SDK's typed list-with-metadata
        // wrapper. For the lesson we just .Add into it.
        var list = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var text in values)
        {
            var vec = new float[dimensions];

            // `text.Split([' ', '.', ',', '?'], StringSplitOptions.RemoveEmptyEntries)`
            // -- splits on any of the listed chars (the first parameter is a
            // char array via collection expression) and drops empty pieces.
            foreach (var word in text.Split([' ', '.', ',', '?'], StringSplitOptions.RemoveEmptyEntries))
            {
                // `TryGetValue(key, out value)` returns true and assigns
                // value if the key exists. The TERNARY picks the topic slot
                // for known words, otherwise hashes into the "rest" of the
                // vector (slots 3..dim-1) so unrelated words don't collide
                // with the topic dims.
                int slot = _topicBias.TryGetValue(word, out var t)
                    ? t                                                      // topic-biased dim
                    : (Math.Abs(word.GetHashCode()) % (dimensions - 3)) + 3; // generic dim
                vec[slot] += 1f;
            }

            // `new Embedding<float>(vec)` boxes the raw float[] into the
            // SDK's strongly-typed embedding container.
            list.Add(new Embedding<float>(vec));
        }
        return Task.FromResult(list);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
