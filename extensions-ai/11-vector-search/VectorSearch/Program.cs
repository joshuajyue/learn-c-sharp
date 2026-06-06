// Lesson 11: Vector search -- from linear scan to approximate nearest neighbour
// ============================================================================
//
// THE PROBLEM
// ----------------------------------------------------------------------------
// Lesson 10 built one embedding per document and scanned ALL of them to find
// the nearest neighbours of a query. With 4 documents that's free. With 4
// million 1536-dimensional vectors, computing 4 million dot products per
// query is way too slow for an interactive app -- typically 100 ms+ on
// commodity hardware, and you usually want < 20 ms.
//
// VECTOR SEARCH is the discipline of finding nearest neighbours in
// high-dimensional space QUICKLY. It's a sub-field of information retrieval
// with decades of literature; the modern AI explosion has pushed it into
// every backend stack.
//
//
// THREE FAMILIES YOU SHOULD KNOW
// ----------------------------------------------------------------------------
//   1. EXACT SEARCH (the lesson-10 strategy):
//      compute similarity to every vector. Cost: O(N * D).
//      Correct but linear in corpus size. Viable up to ~10-100k vectors
//      depending on D and latency budget, especially with SIMD.
//
//   2. INVERTED FILE INDEX (IVF) -- this lesson:
//      pre-cluster all vectors into K "buckets" (using e.g. k-means).
//      A query is compared against the K bucket CENTROIDS, then only the
//      `nprobe` most-promising buckets are scanned. Cost roughly
//      O(K + (N/K) * D * (nprobe/K)).
//      Tunable: more buckets probed = better recall but slower.
//
//   3. HNSW (Hierarchical Navigable Small World):
//      build a multi-layer "small world" graph. Search by greedy walks on
//      each layer, descending to denser layers. SUB-LINEAR query time.
//      The de-facto default in modern vector DBs (Qdrant, Milvus, pgvector
//      with hnsw, Azure AI Search, Pinecone).
//
// 2 and 3 are APPROXIMATE -- they may miss the true top-K once in a while.
// In return you get 10-1000x speedups. For RAG that trade-off is fine; for
// "find the exact duplicate" you'd stick with exact search.
//
//
// WHAT YOU'D ACTUALLY USE IN PRODUCTION
// ----------------------------------------------------------------------------
// FAISS is the classic C++ library implementing all of the above (Facebook
// AI Research, 2017). On .NET you typically use a vector DB through a
// connector -- Qdrant, Azure AI Search, pgvector + Npgsql, etc. The new
// `Microsoft.Extensions.VectorData` abstractions standardise that across
// providers (same "one interface, many implementations" play as
// IChatClient). M.E.AI itself does NOT ship an in-memory vector store.
//
//
// WHAT WE BUILD HERE
// ----------------------------------------------------------------------------
// A tiny IVF index in pure C# so you can see the algorithm with your eyes.
// To make the demo repeatable, we hand-pick the 4 centroids (one per topic
// group) instead of running k-means. The classifier and the search logic
// are the real thing -- in production you'd swap the k-means step in and
// the rest is unchanged.
//
//
// WHY A FAKE EMBEDDER?
// ----------------------------------------------------------------------------
// Same trick as lesson 10: a hash-based embedder with topic bias so that
// words in the same conceptual group share dimensions. The IVF math is the
// real lesson; the embedder is just a deterministic stand-in.

using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> embedder = new HashEmbeddingGenerator(dim: 64);

// A small corpus, deliberately built from FOUR topical groups (cats, dogs,
// SQL, Italian food) of 3 sentences each. We'll seed one centroid per group
// below so the IVF buckets line up with topics.
string[] corpus =
[
    "Cats sleep about 15 hours per day.",
    "Kittens are very playful and curious.",
    "Felines groom themselves frequently.",
    "Dogs love to fetch balls in the park.",
    "Puppies need lots of socialization.",
    "Retrievers are popular family pets.",
    "SQL indexes can dramatically speed up queries.",
    "B-tree indexes are the default in most databases.",
    "Use EXPLAIN to inspect a query plan.",
    "Spaghetti carbonara uses eggs, not cream.",
    "Pasta should be cooked al dente.",
    "Italian cuisine has many regional styles.",
];

// Embed the whole corpus in one batched call. The result is a sequence of
// `Embedding<float>`; we project to `(int id, float[] vec)` tuples for the
// index. `.Vector.ToArray()` copies the underlying ReadOnlyMemory<float>
// into a fresh array we can store. `Select((e, i) => ...)` is the LINQ
// overload that gives you the element AND its index.
var docEmbeddings = (await embedder.GenerateAsync(corpus))
    .Select((e, i) => (id: i, vec: e.Vector.ToArray())).ToList();

// Build the IVF index. We "cheat" by hand-picking the first doc of each
// topical group as that group's centroid (0, 3, 6, 9). In real code you'd
// run k-means on the embeddings and let it discover clusters.
//
// `nprobe = 2` means each query will scan only the 2 best-matching buckets
// out of 4. Smaller nprobe = faster but less likely to hit the true best.
var centroidIds = new[] { 0, 3, 6, 9 };
var index = new IvfIndex(dim: 64, nprobe: 2);
foreach (var cid in centroidIds)
{
    index.AddCentroid(docEmbeddings[cid].vec);
}
foreach (var (id, vec) in docEmbeddings)
{
    index.Add(id, vec);
}

// One query, two strategies. Print both rankings to see whether IVF agreed
// with exact search (it should here; if you push nprobe down to 1 you'll
// see them diverge for queries that straddle topics).
string query = "How do I make my database queries run faster?";
var queryVec = (await embedder.GenerateAsync([query])).First().Vector.ToArray();

var exactTop = Search.LinearTop(docEmbeddings, queryVec, k: 3);
var ivfTop   = index.Search(queryVec, k: 3);

Console.WriteLine($"query: {query}\n");
Console.WriteLine("exact top-3 (scans all):");
foreach (var (id, score) in exactTop) Console.WriteLine($"  {score, 6:F3}  {corpus[id]}");
Console.WriteLine($"\nIVF top-3 (scans {index.LastScanned}/{corpus.Length}):");
foreach (var (id, score) in ivfTop)   Console.WriteLine($"  {score, 6:F3}  {corpus[id]}");


// --- IVF (Inverted File Index) ---------------------------------------------
//
// Two stages per query:
//   (A) compare query against the K centroids to pick the top `nprobe`,
//   (B) scan ONLY the vectors that landed in those buckets.
//
// The fewer buckets you probe, the faster but the lower the recall.
//
// PRIMARY CONSTRUCTOR (C# 12): `(int dim, int nprobe)` are parameters in
// scope across the whole class body -- see lesson 08 for the longer
// explanation of primary ctors.
internal sealed class IvfIndex(int dim, int nprobe)
{
    // `[]` is the collection expression for an empty `List<T>`. Equivalent
    // long form: `new List<float[]>()`.
    private readonly List<float[]> _centroids = [];

    // Dictionary keyed by centroid index -> the list of (id, vec) assigned
    // to that bucket. Same shape as Java `Map<Integer, List<Pair<Integer, float[]>>>`.
    private readonly Dictionary<int, List<(int id, float[] vec)>> _buckets = [];

    // Auto-implemented PROPERTY with a public getter and a PRIVATE setter --
    // any code can read it; only this class can write it. Useful for
    // "diagnostic" fields like "how many vectors did the last query scan?".
    public int LastScanned { get; private set; }

    public void AddCentroid(float[] centroid)
    {
        // The bucket for centroid index N is created when the centroid is added.
        _buckets[_centroids.Count] = [];
        _centroids.Add(centroid);
    }

    public void Add(int id, float[] vec)
    {
        // Standard "assign to nearest centroid" loop. `float.MinValue` as a
        // sentinel for "no centroid scored higher than this yet" -- works
        // because cosine similarities are in [-1, 1].
        int best = 0; float bestScore = float.MinValue;
        for (int c = 0; c < _centroids.Count; c++)
        {
            var s = Cosine(_centroids[c], vec);
            if (s > bestScore) { bestScore = s; best = c; }
        }
        _buckets[best].Add((id, vec));
    }

    public IEnumerable<(int id, float score)> Search(float[] query, int k)
    {
        // Stage A: rank centroids by similarity to the query.
        //
        // `Enumerable.Range(0, n)` yields 0, 1, ..., n-1. The chain projects
        // each index to a `(c, score)` tuple, sorts by score descending,
        // takes the top `nprobe`, and projects back down to just the index.
        // This LINQ-pipeline pattern (`Select.OrderBy.Take.Select`) is the
        // standard C# idiom for "give me the indices of the K best things".
        var topCentroids = Enumerable.Range(0, _centroids.Count)
            .Select(c => (c, score: Cosine(_centroids[c], query)))
            .OrderByDescending(x => x.score)
            .Take(nprobe)
            .Select(x => x.c)
            .ToList();

        // Stage B: flatten the chosen buckets into one candidate list, then
        // rank those candidates exhaustively. `SelectMany` is the LINQ
        // flatMap -- "for each centroid c, give me every (id, vec) in its
        // bucket" produces one combined sequence.
        var candidates = topCentroids.SelectMany(c => _buckets[c]).ToList();
        LastScanned = candidates.Count;

        return candidates
            .Select(p => (p.id, score: Cosine(p.vec, query)))
            .OrderByDescending(p => p.score)
            .Take(k);
    }

    // Cosine similarity, repeated as a private static on each helper class
    // to keep them independent. Production code would put this in one shared
    // utility; we keep it local so each file reads top-to-bottom.
    private static float Cosine(float[] a, float[] b)
    {
        float dot = 0, ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; ma += a[i] * a[i]; mb += b[i] * b[i]; }
        return dot / (MathF.Sqrt(ma) * MathF.Sqrt(mb) + 1e-9f);
    }
}

// --- Plain exact-search helper, for the comparison print ------------------
//
// `internal static class` -- a STATIC CLASS can't be instantiated and can
// only contain static members. Java's equivalent is `final class` + private
// ctor, or a class of static methods. Use this for "namespace of functions".
internal static class Search
{
    public static IEnumerable<(int id, float score)> LinearTop(
        List<(int id, float[] vec)> docs, float[] q, int k)
    {
        return docs
            .Select(d => (d.id, score: Cosine(d.vec, q)))
            .OrderByDescending(d => d.score)
            .Take(k);
    }

    private static float Cosine(float[] a, float[] b)
    {
        float dot = 0, ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; ma += a[i] * a[i]; mb += b[i] * b[i]; }
        return dot / (MathF.Sqrt(ma) * MathF.Sqrt(mb) + 1e-9f);
    }
}


// --- Same toy embedder pattern as lesson 10: hash + topic bias --------------
//
// Real production uses a real embedding model behind
// `IEmbeddingGenerator<string, Embedding<float>>` -- the interface is
// identical; only the implementation changes.
internal sealed class HashEmbeddingGenerator(int dim) : IEmbeddingGenerator<string, Embedding<float>>
{
    // Topic bias map -- words in the same conceptual group share a slot.
    // (Indices 0..3 are reserved for topic dims; the rest are hash-spread.)
    private static readonly Dictionary<string, int> _topic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cat"]=0,["cats"]=0,["kitten"]=0,["kittens"]=0,["feline"]=0,["felines"]=0,["groom"]=0,["sleep"]=0,
        ["dog"]=1,["dogs"]=1,["puppy"]=1,["puppies"]=1,["retriever"]=1,["retrievers"]=1,["fetch"]=1,["park"]=1,
        ["sql"]=2,["query"]=2,["queries"]=2,["database"]=2,["databases"]=2,["index"]=2,["indexes"]=2,["btree"]=2,["b-tree"]=2,["explain"]=2,["faster"]=2,["run"]=2,
        ["pasta"]=3,["spaghetti"]=3,["carbonara"]=3,["italian"]=3,["cuisine"]=3,["dente"]=3,
    };

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var text in values)
        {
            var v = new float[dim];
            foreach (var raw in text.Split([' ', '.', ',', '?', '!', '-'], StringSplitOptions.RemoveEmptyEntries))
            {
                var w = raw.ToLowerInvariant();
                int slot = _topic.TryGetValue(w, out var t) ? t : (Math.Abs(w.GetHashCode()) % (dim - 4)) + 4;
                v[slot] += 1f;
            }
            list.Add(new Embedding<float>(v));
        }
        return Task.FromResult(list);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
