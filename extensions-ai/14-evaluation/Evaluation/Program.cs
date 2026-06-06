// Lesson 14: Evaluating AI quality (the RAG triad)
// ============================================================================
//
// WHY EVALUATION EXISTS
// ----------------------------------------------------------------------------
// "It worked on my laptop" doesn't survive contact with real users in an AI
// product. Two properties of LLM systems make traditional testing
// insufficient:
//
//   * STOCHASTICITY. The same prompt can produce different outputs on
//     different calls (unless temperature is zero AND the provider is
//     deterministic -- rare). One green run proves nothing.
//
//   * SUBJECTIVITY. "Is this answer good?" doesn't have a single
//     pass/fail line. It has axes: was it grounded? did it actually answer
//     the question? was the tone appropriate? did it cite sources?
//
// EVALUATION is the practice of measuring those axes REPEATABLY across a
// representative set of cases, so you can tell if a prompt change, a model
// swap, or a retrieval-tuning experiment made the system better or worse.
// Without it you're flying blind -- every change is an opinion.
//
//
// THE RAG TRIAD
// ----------------------------------------------------------------------------
// For RAG (Retrieval-Augmented Generation) systems specifically, the
// industry-standard quality scorecard is THREE independent metrics. Each
// fails for a DIFFERENT reason, so isolating them lets you find the broken
// component instead of guessing.
//
//   1. CONTEXT RELEVANCE  (a.k.a. "Retrieval")
//        Did the retriever fetch chunks that are RELATED to the question?
//        Failure mode: the embedding model or vector search is broken.
//        Tested with: question + retrieved chunks (NO answer).
//
//   2. GROUNDEDNESS
//        Is every claim in the answer SUPPORTED by the retrieved context?
//        Failure mode: the LLM hallucinated or embellished.
//        Tested with: retrieved chunks + answer (NO question needed).
//
//   3. ANSWER RELEVANCE
//        Did the answer actually ANSWER the user's question?
//        Failure mode: grounded but off-topic ("you asked about cats; here's
//        a true fact about dogs").
//        Tested with: question + answer (NO context needed).
//
// Why three and not one? Because a HIGH-scoring answer in only some of them
// is diagnostic:
//   * High retrieval + low groundedness -> the LLM is hallucinating.
//   * Low retrieval + everything else low -> fix the retriever first.
//   * High groundedness + low relevance -> the answer is truthful but missed
//     the point; usually a prompt-engineering fix.
//
//
// "LLM AS JUDGE"
// ----------------------------------------------------------------------------
// The standard implementation pattern: a STRONG LLM (preferably stronger
// than the system under test) reads the question, the context, the answer,
// and a RUBRIC, then outputs a numeric score plus reasoning. This is called
// "LLM as judge" and is the basis for every modern eval framework.
//
// Why an LLM judge instead of code? Because the metrics above are SEMANTIC
// -- "did this address the question?" is exactly the kind of fuzzy
// judgement language models excel at. Rule-based heuristics (BLEU, ROUGE,
// keyword overlap) miss the point.
//
//
// HOW M.E.AI MODELS THIS
// ----------------------------------------------------------------------------
// Microsoft.Extensions.AI.Evaluation provides the abstraction layer:
//
//   IEvaluator                  -- the interface every metric implements
//   EvaluationResult            -- a bag of named metric values + reasons
//   NumericMetric / *Metric     -- typed metric subclasses
//   EvaluationContext           -- extra inputs (retrieved chunks) the
//                                  caller passes to context-aware evaluators
//   ChatConfiguration           -- carries the judge IChatClient
//
// Source:
//   src/Libraries/Microsoft.Extensions.AI.Evaluation/IEvaluator.cs
//
// Microsoft.Extensions.AI.Evaluation.Quality (a SEPARATE package, NOT
// referenced by this lesson) ships hardened evaluators built on top of
// that abstraction: GroundednessEvaluator, RelevanceEvaluator,
// RetrievalEvaluator, CoherenceEvaluator, FluencyEvaluator, etc.
//
// WHY WE ROLL OUR OWN HERE.
//   The Quality package's evaluators drive the judge LLM with carefully
//   tuned prompts and expect the judge to reply in a SPECIFIC JSON SHAPE so
//   the evaluator can parse it. Wiring those expectations into a FAKE judge
//   for a self-contained lesson is more confusing than illuminating. Our
//   custom `JudgeEvaluator` base class shows you EXACTLY what an evaluator
//   IS -- prompt the judge, parse a 1-5 score, return a NumericMetric --
//   so in production you can confidently swap our four classes for the
//   package's hardened versions WITHOUT changing the loop below.
//
//
// WHY FAKES?
// ----------------------------------------------------------------------------
// `FakeJudge` is a deterministic scorer that ROUTES on rubric keywords
// (e.g. "supported by the context" -> apply groundedness logic) and assigns
// scores that match what a real judge would say on these test cases. That
// lets you SEE meaningful triad scores (a 5/5/5 case, a low-groundedness
// case, a retrieval-failure case) without needing a real LLM.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

// The "judge" model. In production this is a strong LLM (gpt-4o, Claude
// Opus, ...). The SAME model as the system-under-test works for cheap eval;
// a STRONGER judge is best practice -- you want the grader smarter than the
// student.
//
// `ChatConfiguration` is the small DTO that every evaluator takes; it
// bundles the judge `IChatClient` (and optionally an embedding generator,
// for evaluators that need similarity).
IChatClient judge = new FakeJudge();
var chatConfig = new ChatConfiguration(judge);

// One evaluator per metric. Each is REUSABLE across many test cases --
// they hold no per-case state, just the rubric template.
var groundedness = new SimpleGroundednessEvaluator();
var relevance    = new SimpleAnswerRelevanceEvaluator();
var retrieval    = new SimpleContextRelevanceEvaluator();

// Three test cases chosen so the triad LIGHTS UP DIFFERENTLY for each:
//   #1 -- perfect: 5/5/5
//   #2 -- a hallucinated detail; groundedness should drop
//   #3 -- off-topic context; retrieval should drop, refusal hurts relevance
var cases = new TestCase[]
{
    new("How often should senior cats see the vet?",
        Context: ["Senior cats over ten should see a vet twice a year.", "Adult cats eat two meals a day."],
        Answer: "Twice a year for cats older than ten."),

    new("How often should senior cats see the vet?",
        Context: ["Senior cats over ten should see a vet twice a year."],
        // The "monthly dental cleaning" claim is NOT in the context.
        // Expect groundedness to drop while the other two stay high.
        Answer: "Twice a year, and they should also have a dental cleaning monthly."),

    new("What's the capital of France?",
        // Retrieval failure: the chunks are about cats. The bot dutifully
        // refuses -- so retrieval is low (off-topic context) AND relevance
        // is low (didn't answer), but groundedness is technically HIGH
        // because refusal is grounded ("I don't know" invents nothing).
        Context: ["Cats are obligate carnivores."],
        Answer: "I don't know based on the provided sources."),
};

foreach (var c in cases)
{
    // Each evaluator wants:
    //   * the CONVERSATION (typically a one-message [user question]),
    //   * the candidate RESPONSE (assistant message), and
    //   * for context-dependent metrics, an "evaluator context" carrying
    //     the retrieved chunks via the `EvaluationContext` base class.
    var conversation = new[] { new ChatMessage(ChatRole.User, c.Question) };
    var response     = new ChatResponse(new ChatMessage(ChatRole.Assistant, c.Answer));
    var ctx          = new ChunksContext(c.Context);

    // Run the three evaluators. Notice the consistency: the SAME shape of
    // call works for every metric. That uniformity is exactly what
    // `IEvaluator` buys you -- you can plug new metrics into a loop without
    // refactoring the caller.
    var rGround = await groundedness.EvaluateAsync(conversation, response, chatConfig, [ ctx ]);
    var rRel    = await relevance.EvaluateAsync(conversation, response, chatConfig);
    var rRetr   = await retrieval.EvaluateAsync(conversation, response, chatConfig, [ ctx ]);

    Console.WriteLine($"Q: {c.Question}");
    Console.WriteLine($"A: {c.Answer}");
    Print("groundedness", rGround);
    Print("relevance   ", rRel);
    Print("retrieval   ", rRetr);
    Console.WriteLine();
}

// LOCAL FUNCTION (lesson 04). Walks the metrics dictionary, filters to
// `NumericMetric` (other metric types include `StringMetric` for
// classifications), and prints "metric: 4.0/5  (because the judge said X)".
//
// `,3` in the format string is a WIDTH SPECIFIER -- pads the value to 3
// chars so columns line up. Same as `String.format("%3s", v)` in Java.
static void Print(string label, EvaluationResult result)
{
    foreach (var metric in result.Metrics.Values.OfType<NumericMetric>())
    {
        // `NumericMetric.Value` is `double?` (nullable double). `?.ToString`
        // returns null if Value is null; `??` then falls back to "?".
        // `"F1"` is the format spec for "fixed-point, 1 digit after the
        // decimal" (so 4 -> "4.0").
        var v = metric.Value?.ToString("F1") ?? "?";
        Console.WriteLine($"  {label}: {v,3}/5   ({metric.Reason})");
    }
}


// Plain record: question + context chunks + candidate answer. `string[]`
// is the BCL array type; equivalent to Java's `String[]`. Records get
// VALUE EQUALITY for free (so two test cases with the same data compare
// equal), which is occasionally useful when deduplicating eval sets.
internal record TestCase(string Question, string[] Context, string Answer);


// --- Evaluator context: carries the retrieved chunks to context-aware
// evaluators (groundedness, retrieval/context-relevance).
//
// `EvaluationContext` is the base class M.E.AI.Evaluation provides for
// "extra inputs an evaluator might need". The real package has dedicated
// subclasses (`GroundednessEvaluatorContext`, `RetrievalEvaluatorContext`);
// we collapse them into one to keep the lesson focused.
//
// `name`/`content` are passed to the base ctor and are what gets logged
// to the disk-based evaluation report (if you write one).
// `Chunks` is our typed accessor on top of `content`.
//
// `(string[] chunks)` after the class name is a PRIMARY CONSTRUCTOR
// (C# 12) -- `chunks` is in scope inside the class body, including the
// base ctor call.
internal sealed class ChunksContext(string[] chunks)
    : EvaluationContext(name: "RetrievedChunks", content: string.Join("\n", chunks))
{
    public string[] Chunks { get; } = chunks;
}


// --- Custom evaluators ---------------------------------------------------
//
// Each one:
//   1. Builds a RUBRIC PROMPT for the judge LLM (the "you are a strict
//      grader, output a 1-5 score" template).
//   2. ASKS the judge for a score.
//   3. PARSES the first 1-5 digit in the reply into a `NumericMetric`.
//
// The real Microsoft.Extensions.AI.Evaluation.Quality evaluators do the
// SAME thing with more sophisticated prompts and JSON parsing. The shape
// of the work is identical -- which is the whole point of having a
// `IEvaluator` abstraction.
//
// `JudgeEvaluator` is ABSTRACT so subclasses must supply `BuildPrompt`
// (the rubric). Same idea as Java's `abstract class` with one abstract
// method to override.
internal abstract class JudgeEvaluator(string metricName) : IEvaluator
{
    // `IReadOnlyCollection<string>` is the typed view of a single-element
    // collection. Java analogue: `Collections.unmodifiableList(...)`.
    // `[metricName]` is the collection-expression syntax for a one-element
    // list with the inferred element type.
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [metricName];
    protected string MetricName { get; } = metricName;

    protected abstract string BuildPrompt(string question, string answer, string[] chunks);

    // `ValueTask<T>` (vs `Task<T>`) is the "might complete synchronously"
    // task type -- saves an allocation when the work is already done.
    // The framework picked it for `IEvaluator` because evaluators
    // sometimes can return immediately (e.g. on missing inputs).
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        if (chatConfiguration is null)
            throw new InvalidOperationException("Judge required.");

        // Pull out the question, candidate answer, and optional chunks.
        // `messages.Last(m => m.Role == ChatRole.User).Text` -- LINQ
        // `Last(predicate)` throws if nothing matches; safe because we
        // built the conversation with a user message in the loop above.
        //
        // `additionalContext?.OfType<ChunksContext>().FirstOrDefault()
        //  ?.Chunks ?? []` -- a chain of nullables: if the caller passed
        // additional context, find the first `ChunksContext`, get its
        // `Chunks`. If anything in the chain is null, fall back to the
        // empty array `[]`.
        var question = messages.Last(m => m.Role == ChatRole.User).Text;
        var answer   = modelResponse.Text;
        var chunks   = additionalContext?.OfType<ChunksContext>().FirstOrDefault()?.Chunks ?? [];

        var prompt = BuildPrompt(question, answer, chunks);
        var judgeReply = await chatConfiguration.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)], cancellationToken: cancellationToken);

        // TOY PARSER: scan the judge's reply for the first digit 1-5.
        // Production parsers ask the judge for JSON and parse that --
        // safer when the judge gets chatty. `is >= '1' and <= '5'` is the
        // C# 9 pattern-match form ("relational and combined patterns").
        var raw = judgeReply.Text;
        double? score = null;
        foreach (var ch in raw)
            if (ch is >= '1' and <= '5') { score = ch - '0'; break; }

        // OBJECT INITIALIZER `{ Reason = raw.Trim() }` (lesson 01) sets
        // public properties after construction without an explicit setter
        // call. The metric carries both the numeric score AND the judge's
        // reasoning -- crucial for debugging "why did this score drop?".
        return new EvaluationResult(new NumericMetric(MetricName, value: score) { Reason = raw.Trim() });
    }
}

// Three concrete evaluators, one per RAG-triad metric. Each defines its
// rubric prompt as a RAW STRING LITERAL (triple-quote `"""..."""`, lesson
// 05). The `$$"""..."""` form requires DOUBLE braces (`{{` and `}}`) for
// interpolation -- a thoughtful syntax tweak so prompts with single braces
// in them (JSON examples, format specs) don't trip the parser.
internal sealed class SimpleGroundednessEvaluator() : JudgeEvaluator("Groundedness")
{
    protected override string BuildPrompt(string q, string a, string[] chunks) => $$"""
        You are a strict grader. Score how well the ANSWER is SUPPORTED by the CONTEXT.
        5 = every claim is supported.  1 = mostly hallucinated.
        Reply with one digit 1-5 then a short reason.

        CONTEXT:
        {{string.Join("\n", chunks)}}

        ANSWER: {{a}}
        """;
}

internal sealed class SimpleAnswerRelevanceEvaluator() : JudgeEvaluator("Relevance")
{
    protected override string BuildPrompt(string q, string a, string[] chunks) => $$"""
        You are a strict grader. Score how well the ANSWER addresses the QUESTION.
        5 = directly answers.  1 = doesn't address it.
        Reply with one digit 1-5 then a short reason.

        QUESTION: {{q}}
        ANSWER:   {{a}}
        """;
}

internal sealed class SimpleContextRelevanceEvaluator() : JudgeEvaluator("Retrieval")
{
    protected override string BuildPrompt(string q, string a, string[] chunks) => $$"""
        You are a strict grader. Score how relevant the retrieved CONTEXT is to the QUESTION.
        5 = highly relevant.  1 = off-topic.
        Reply with one digit 1-5 then a short reason.

        QUESTION: {{q}}
        CONTEXT:
        {{string.Join("\n", chunks)}}
        """;
}


// --- Fake judge ----------------------------------------------------------
//
// Scores by KEYWORD HEURISTIC so the lesson runs offline. The router keys
// off the rubric's distinctive phrase ("supported by the context",
// "addresses the question", "relevant the retrieved context"), then
// applies metric-appropriate logic to the test data.
//
// A real judge LLM (gpt-4o, claude-3.5-sonnet) reads each prompt, reasons,
// and outputs a calibrated score. Swap `new FakeJudge()` at the top of the
// file for `OpenAIClient(key).AsIChatClient()` (or any provider) and you
// have a real evaluation harness.
internal sealed class FakeJudge : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var p = messages.Last().Text.ToLowerInvariant();

        string reply;
        // GROUNDEDNESS: punish the hallucinated dental-cleaning claim.
        if (p.Contains("supported by the context"))
        {
            reply = p.Contains("dental cleaning monthly")
                ? "2  -- 'monthly dental cleaning' is not in the context"
                : p.Contains("i don't know")
                    ? "5  -- refuses to invent; trivially grounded"
                    : "5  -- every claim is in the context";
        }
        // ANSWER RELEVANCE: punish the I-don't-know to "capital of France".
        else if (p.Contains("addresses the question"))
        {
            reply = p.Contains("don't know")
                ? "2  -- abstains instead of answering"
                : "5  -- directly answers";
        }
        // CONTEXT RELEVANCE (retrieval): punish off-topic context.
        else if (p.Contains("relevant the retrieved context"))
        {
            reply = p.Contains("capital of france") && p.Contains("obligate carnivores")
                ? "1  -- context is about cats; question is about geography"
                : "5  -- context covers the question";
        }
        else reply = "3  -- unrecognised rubric";

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(IChatClient) && serviceKey is null ? this : null;

    public void Dispose() { }
}
