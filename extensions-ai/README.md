# extensions-ai/

Learning **Microsoft.Extensions.AI** (a.k.a. M.E.AI) on **.NET 10** -- basics only.

> **What is Microsoft.Extensions.AI?** A set of `Microsoft.Extensions.*` packages
> that define common abstractions for AI services (chat, embeddings, tools,
> function calling, telemetry) the same way `Microsoft.Extensions.Logging`
> defines common abstractions for logging. You write code against `IChatClient`
> and `IEmbeddingGenerator<TInput, TEmbedding>`; you swap the underlying
> provider (OpenAI, Azure OpenAI, Ollama, Anthropic, a local fake) without
> touching the consumer.
>
> Source repo: https://github.com/dotnet/extensions
> -- specifically `src/Libraries/Microsoft.Extensions.AI{,.Abstractions}/`.

Every lesson **runs offline with no API key** -- each one implements a tiny
fake `IChatClient` (or `IEmbeddingGenerator`, or `ISpeechToTextClient`) so
you can see the abstraction shape without paying for tokens or installing
Ollama. Lesson 09 shows how to swap in a real provider when you're ready.

## Packages used

| Package                              | Lessons      | Purpose                                |
|--------------------------------------|--------------|----------------------------------------|
| `Microsoft.Extensions.AI`            | all (10.6.0) | High-level: `ChatClientBuilder`, `UseFunctionInvocation`, `UseDistributedCache`, etc. |
| `Microsoft.Extensions.AI.Abstractions` | (transitive) | The interfaces: `IChatClient`, `IEmbeddingGenerator<,>`, `ChatMessage`, `AIFunction`, `ISpeechToTextClient`, ... |
| `Microsoft.Extensions.AI.Evaluation` | 14 (10.6.0)  | `IEvaluator`, `EvaluationResult`, `NumericMetric`, `ChatConfiguration`. |
| `Microsoft.Extensions.Hosting`       | 09 (9.0.0)   | `Host.CreateApplicationBuilder` for the DI sample. |
| `Microsoft.Extensions.Caching.Memory`| 15 (10.0.6)  | `MemoryDistributedCache` (in-memory `IDistributedCache`). |

## Lesson map

The 16 lessons map roughly to the 8 modules of the public Microsoft AI Workshop
(WS1-WS8). The "WS" column tells you which workshop topic each lesson advances.

| #  | Folder                       | Project              | WS  | Concept |
|----|------------------------------|----------------------|-----|---------|
| 1  | `01-hello-chat-client`       | `HelloChatClient`    |     | `IChatClient`, `ChatMessage`, `ChatRole`, `ChatResponse`, `UsageDetails`. |
| 2  | `02-streaming`               | `StreamingChat`      |     | `GetStreamingResponseAsync` + `IAsyncEnumerable<ChatResponseUpdate>`. |
| 3  | `03-conversation-history`    | `Conversation`       |     | LLMs are stateless; the conversation IS the message list. |
| 4  | `04-chat-options`            | `ChatOptionsLesson`  | WS4 | `ChatOptions`: Temperature, MaxOutputTokens, `ChatResponseFormat.Json`. |
| 5  | `05-prompt-engineering`      | `PromptEngineering`  | WS1 | System prompts, few-shot, output validation, **prompt-injection** defence (quiz app). |
| 6  | `06-structured-output`       | `StructuredOutput`   | WS4 | `GetResponseAsync<T>` → typed records via auto-generated JSON schema. |
| 7  | `07-tools-function-calling`  | `FunctionCalling`    | WS5 | `AIFunctionFactory.Create`, `FunctionCallContent`, `UseFunctionInvocation()`. |
| 8  | `08-middleware-pipeline`     | `ChatMiddleware`     | WS5 | `ChatClientBuilder.Use(...)` + `DelegatingChatClient` for cross-cutting concerns. |
| 9  | `09-di-and-providers`        | `DiAndProviders`     |     | `AddChatClient(...)`, injecting `IChatClient`, swapping to OpenAI / Ollama. |
| 10 | `10-embeddings`              | `EmbeddingsBasics`   | WS2 | `IEmbeddingGenerator<string, Embedding<float>>`, cosine similarity. |
| 11 | `11-vector-search`           | `VectorSearch`       | WS3 | IVF / partition-and-probe index; exact-scan vs ANN trade-off. |
| 12 | `12-rag-ingestion`           | `RagIngestion`       | WS6 | Recursive chunking with overlap; per-chunk metadata for citations. |
| 13 | `13-rag-retrieval`           | `RagRetrieval`       | WS6 | End-to-end Q&A: embed → retrieve → grounded prompt → cited answer (or "I don't know"). |
| 14 | `14-evaluation`              | `Evaluation`         | WS6 | **RAG triad** -- groundedness, answer-relevance, context-relevance via custom `IEvaluator`s. |
| 15 | `15-vision-and-caching`      | `VisionAndCaching`   | WS7 | `DataContent` images (multi-modal) + `UseDistributedCache(IDistributedCache)`. |
| 16 | `16-speech-and-realtime`     | `SpeechAndRealtime`  | WS8 | `ISpeechToTextClient` (blocking + streaming) → pointer to Realtime audio APIs. |

## Running

```pwsh
cd extensions-ai/01-hello-chat-client/HelloChatClient
dotnet run
```

## What's intentionally NOT here

These are the next steps once the 16 basics click -- not in this track:

- **Real provider wire-up.** Lesson 09 shows the one-liner; add the
  appropriate package (`Microsoft.Extensions.AI.OpenAI`, `OllamaSharp`, etc.)
  and a key/endpoint when you want to talk to a real model.
- **`Microsoft.Extensions.AI.Evaluation.Quality`** -- the production-grade
  evaluators (`GroundednessEvaluator`, `RelevanceEvaluator`, ...) with
  carefully-tuned judge prompts. Lesson 14 hand-rolls equivalents so you
  can see the shape.
- **`Microsoft.Extensions.VectorData`** + a real vector DB (Azure AI Search,
  Qdrant, Pinecone) instead of the in-memory IVF index from lesson 11.
- **`Microsoft.Extensions.DataIngestion`** -- production chunking and
  ingestion for RAG; lesson 12 hand-rolls a recursive chunker.
- **MCP servers** (`Microsoft.Extensions.AI.MCP`) -- tools-as-a-service.
- **OpenTelemetry** -- `UseOpenTelemetry()` for request tracing across the
  pipeline.
- **Realtime audio** -- lesson 16 points at the Realtime APIs but doesn't
  open a session; that needs a real provider that supports audio-to-audio.
