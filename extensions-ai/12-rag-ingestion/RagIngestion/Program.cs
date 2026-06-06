// Lesson 12: RAG, part 1 -- ingestion and chunking
// ============================================================================
//
// WHAT IS RAG?
// ----------------------------------------------------------------------------
// RAG = Retrieval-Augmented Generation. The technique that lets an LLM
// answer questions about YOUR documents (or your database, or any text
// corpus you own) WITHOUT retraining the model.
//
// The flow has two phases. This lesson covers the first; lesson 13 covers
// the second.
//
//   INGESTION (offline; once per document update):
//     load -> clean -> CHUNK -> embed each chunk -> store (vector + metadata)
//
//   QUERY TIME (online; per user question -- lesson 13):
//     embed question -> nearest chunks -> stuff into prompt -> generate
//
// Everything before "embed each chunk" is plumbing -- file IO, HTML
// stripping, OCR, table extraction. The two HARD parts that determine RAG
// quality are:
//
//   * CHUNKING -- how do you split a long document into pieces?
//   * METADATA -- what do you store alongside each chunk so you can cite
//                 the source later? (no metadata = no citations = no trust.)
//
//
// WHY CHUNK AT ALL?
// ----------------------------------------------------------------------------
//   * Embedding models CAP INPUT LENGTH. OpenAI's text-embedding-3 line
//     accepts ~8k tokens per call. A 100-page PDF won't fit.
//   * Smaller chunks => SHARPER SIMILARITY SCORES. One vector that averages
//     a whole page is a blurry mess; one per paragraph is precise.
//   * Smaller chunks => LESS WASTED CONTEXT at query time. If you stuff a
//     20-page document into the prompt to answer a one-sentence question
//     you've spent most of your context window on irrelevant text.
//   * BUT: too small and you lose context. If you split mid-sentence, the
//     embedding is meaningless. If you split per-word, every chunk is noise.
//
// "Right-sized" chunks are typically 100-500 tokens (~400-2000 chars), with
// OVERLAP (e.g. 50 chars of trailing text repeated into the next chunk) so
// an answer that straddles a boundary isn't truncated in both halves.
//
//
// FOUR CHUNKING STRATEGIES, IN ORDER OF SOPHISTICATION
// ----------------------------------------------------------------------------
//   1. FIXED-SIZE BY CHARACTER -- split every N chars. Trivial; ignores
//      sentence and paragraph structure; can cut mid-word. Last resort.
//
//   2. SENTENCE OR PARAGRAPH    -- split on `. ! ?` or blank lines. Better
//      semantic boundaries, but chunk size varies wildly: a one-sentence
//      paragraph and a multi-page paragraph both yield exactly one chunk.
//
//   3. RECURSIVE                -- try paragraphs first; if a paragraph is
//      still too big, fall through to sentences; if a sentence is still
//      too big, fall through to fixed-size. The de-facto default (used by
//      LangChain, LlamaIndex, and the chunker below).
//
//   4. STRUCTURAL               -- for source code, split per function or
//      per class; for markdown, per heading; for HTML, per <article> /
//      <section>. Best results when the format gives you obvious cut
//      points. Requires a parser per format.
//
// Always layer OVERLAP on top of whichever strategy you choose.
//
//
// WHAT YOU'D ACTUALLY USE IN PRODUCTION
// ----------------------------------------------------------------------------
// `Microsoft.Extensions.DataIngestion` (in dotnet/extensions) ships
// production ingestion pipelines: chunkers, markdown parsers, and
// `Microsoft.Extensions.DataIngestion.MarkItDown` for PDF/Office. Source:
//   src/Libraries/Microsoft.Extensions.DataIngestion/
//
// We implement a simple recursive chunker here so you can SEE the algorithm
// end-to-end; that package is the path you'd take to ship.
//
//
// WHY A FAKE EMBEDDER (AGAIN)?
// ----------------------------------------------------------------------------
// Same reason as lessons 10 and 11: the lesson runs offline. The chunker is
// the real subject; the embedder just produces deterministic vectors so the
// ingestion pipeline (chunk -> embed -> store) is intact.

using Microsoft.Extensions.AI;

// Raw string literal (lesson 04). Looks like a small markdown doc; we'll
// chunk it into ~200-char pieces with 30-char overlap.
string sourceDoc = """
    # Cat care basics

    Cats are obligate carnivores and need animal protein in their diet.
    Adult cats typically eat two meals a day. Kittens may need three or four.

    Water should always be available. Many cats prefer running water and
    will drink more from a pet fountain than from a still bowl.

    ## Grooming

    Short-haired cats only need brushing weekly. Long-haired breeds like
    Persians need daily brushing to prevent mats.

    ## Veterinary care

    Annual checkups are recommended for adult cats. Seniors over ten should
    see a vet twice a year. Vaccinations include FVRCP and rabies.
    """;

// 200 chars + 30 chars overlap is small for demo purposes; real RAG uses
// 1000-4000 chars (200-800 tokens). The algorithm is identical.
var chunks = RecursiveChunker.Chunk(
    sourceDoc,
    maxChars: 200,
    overlapChars: 30,
    sourceName: "cat-care.md");

IEmbeddingGenerator<string, Embedding<float>> embedder = new HashEmbedder(dim: 32);

// One BATCHED call to embed every chunk -- much cheaper than per-chunk
// calls when the embedder is a real (charged) API. `chunks.Select(c => c.Text)`
// projects to just the text; the result has the same order, so we can `Zip`
// the chunks back with their vectors below.
var vectors = await embedder.GenerateAsync(chunks.Select(c => c.Text));

// In a real app each `IngestedChunk` would be UPSERTED into a vector DB
// (Qdrant, Azure AI Search, pgvector). For the lesson we keep them in
// memory and just print them.
//
// `chunks.Zip(vectors, (chunk, vec) => new IngestedChunk(...))` walks the
// two sequences pairwise (see lesson 10 for `Zip`). Each `IngestedChunk`
// carries:
//   * a unique Id    (source + ordinal -- lesson 13 cites it back)
//   * the raw Text   (we send this to the LLM at query time)
//   * the Source     (filename / URL)
//   * the Ordinal    (position in the source doc; useful for context)
//   * the Vector     (what the vector DB indexes for similarity search)
var corpus = chunks.Zip(vectors, (chunk, vec) => new IngestedChunk(
    Id: $"{chunk.Source}#{chunk.Ordinal}",
    Text: chunk.Text,
    Source: chunk.Source,
    Ordinal: chunk.Ordinal,
    Vector: vec.Vector.ToArray())).ToList();

Console.WriteLine($"produced {corpus.Count} chunks from {sourceDoc.Length} chars\n");
foreach (var c in corpus)
{
    Console.WriteLine($"--- {c.Id} ({c.Text.Length} chars, {c.Vector.Length}-dim vector) ---");
    Console.WriteLine(c.Text);
    Console.WriteLine();
}


// --- The data types --------------------------------------------------------
//
// Records again (lesson 05): immutable-by-default data carriers with value
// equality and a generated ToString. The `Id` field combines source name
// and ordinal so a vector DB can store many documents alongside each other
// without collisions, and lesson 13 can render "[cat-care.md#3]" citations.
public record IngestedChunk(string Id, string Text, string Source, int Ordinal, float[] Vector);
internal record RawChunk(string Text, string Source, int Ordinal);


// --- Recursive chunker -----------------------------------------------------
//
// Three nested strategies in priority order:
//   1. Split on PARAGRAPHS (blank lines).
//   2. For any paragraph still over maxChars, split on SENTENCES.
//   3. For any sentence still over maxChars, fall back to FIXED-SIZE.
//
// Then re-walk the pieces and prepend `overlapChars` of the previous
// piece's tail onto each one. That overlap is the safety net: if a fact
// straddles a chunk boundary, the second chunk repeats enough of the
// neighbouring text that a retrieved match still includes the full claim.
//
// `static class` (lesson 11) makes this a "namespace of functions" -- you
// can't instantiate `RecursiveChunker`, only call its static methods.
internal static class RecursiveChunker
{
    public static List<RawChunk> Chunk(string text, int maxChars, int overlapChars, string sourceName)
    {
        // `["\n\n"]` is a collection-expression `string[]` -- the
        // string-array overload of `Split` matches each FULL string as a
        // separator (rather than splitting on any of the characters, like
        // the char[] overload does). So this splits on blank lines.
        //
        // The fluent LINQ chain `.Split(...).Select(p => p.Trim()).Where(p =>
        // p.Length > 0)` trims whitespace and drops empty fragments. Java
        // Streams equivalent: `.map(String::trim).filter(p -> !p.isEmpty())`.
        var paragraphs = text.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .Where(p => p.Length > 0);

        var pieces = new List<string>();
        foreach (var p in paragraphs)
        {
            // Paragraph already small enough: keep as one piece.
            if (p.Length <= maxChars) { pieces.Add(p); continue; }

            // Fall through to sentences. `AddRange` appends every element of
            // a sequence to the list (Java `addAll(Collection)`).
            foreach (var sentence in SplitSentences(p))
            {
                if (sentence.Length <= maxChars) pieces.Add(sentence);
                else pieces.AddRange(SplitFixed(sentence, maxChars));
            }
        }

        // Re-pack with OVERLAP: each chunk except the first prefixes the
        // last `overlapChars` of the previous piece. The Ordinal is the
        // output index so lesson 13 can reconstruct order.
        var output = new List<RawChunk>();
        for (int i = 0; i < pieces.Count; i++)
        {
            var current = pieces[i];
            if (i > 0)
            {
                var prev = pieces[i - 1];
                // RANGE expression: `prev[Math.Max(0, prev.Length - overlapChars)..]`
                // means "from that index to the end". `Math.Max(0, ...)`
                // guards against tiny previous pieces.
                var tail = prev[Math.Max(0, prev.Length - overlapChars)..];
                current = tail + " " + current;
            }
            output.Add(new RawChunk(current, sourceName, output.Count));
        }
        return output;
    }

    // EXPRESSION-BODIED method returning a query. Each split fragment gets
    // trimmed and has its trailing punctuation restored (we lose the
    // delimiter when `Split` consumes it). The `Where` filters degenerate
    // single-char results.
    private static IEnumerable<string> SplitSentences(string text)
        => text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
               .Select(s => s.Trim() + ".")
               .Where(s => s.Length > 1);

    // ITERATOR METHOD (lesson 02): `yield return` produces values lazily.
    // Walks the string in fixed-size windows, the final one possibly short.
    private static IEnumerable<string> SplitFixed(string text, int size)
    {
        for (int i = 0; i < text.Length; i += size)
            yield return text.Substring(i, Math.Min(size, text.Length - i));
    }
}


// --- Hash embedder ---------------------------------------------------------
//
// Same trick as lessons 10/11, with the topic bias removed (the lesson is
// about chunking, not retrieval). The vectors are deterministic, fast, and
// good enough to demonstrate that ingestion produces (text + vector) pairs.
internal sealed class HashEmbedder(int dim) : IEmbeddingGenerator<string, Embedding<float>>
{
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var text in values)
        {
            var v = new float[dim];
            foreach (var w in text.Split([' ', '.', ',', '?', '!', '-', '\n'], StringSplitOptions.RemoveEmptyEntries))
                v[Math.Abs(w.ToLowerInvariant().GetHashCode()) % dim] += 1f;
            list.Add(new Embedding<float>(v));
        }
        return Task.FromResult(list);
    }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
