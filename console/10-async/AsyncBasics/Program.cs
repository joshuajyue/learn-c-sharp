// Lesson 10: async / await, Task vs ValueTask, cancellation
//
// Mental model:
//   * Java: CompletableFuture + chained .thenApply / .thenCompose. Verbose.
//   * C#:   `async`/`await` is compiler-rewritten to a state machine. The code
//           READS like sequential blocking code but RUNS asynchronously.
//
// Vocabulary:
//   Task         -- "a future that completes later" (reference type, allocated).
//   Task<T>      -- "a future that yields a T".
//   ValueTask<T> -- struct version, avoids allocation on the SYNCHRONOUS-completion
//                   path. Use it for hot APIs that often complete synchronously
//                   (Stream.ReadAsync, ChannelReader.ReadAsync). Rules: await it
//                   exactly once; don't store it.
//   await        -- "if not yet complete, suspend; otherwise grab the result".
//                   On suspend, the rest of the method becomes a continuation
//                   scheduled when the Task completes.

await DemoSequentialAsync();
await DemoParallelAsync();
await DemoCancellationAsync();
DemoValueTask();
await DemoExceptionsAsync();

// --- 1. Sequential awaits ---
// Each await yields the thread back to the caller until the awaited operation
// completes. NOT a new thread per call — usually all on the same thread.

static async Task DemoSequentialAsync()
{
    Console.WriteLine("[seq] start");
    var a = await ReadAsync("A", 50);
    var b = await ReadAsync("B", 50);
    Console.WriteLine($"[seq] got {a} then {b}");
}
// Async is not multithreading:
// The purpose of async: up until a task is blocked, it runs synchronously on the caller's thread
// But, when we hit await of an incomplete task (keyword incomplete: specifically like Task.Delay)
// the method yields control back to the caller.
// When we have async methods, the whole Main becomes async
// SO in this case, ReadAsync is incomplete, which cascades a suspension up. This also suspends Main


// --- 2. Parallel awaits via Task.WhenAll ---
// Kick off both tasks WITHOUT awaiting, then await the combined task. They run
// concurrently. This is the C# equivalent of Java's CompletableFuture.allOf.
static async Task DemoParallelAsync()
{
    Console.WriteLine("[par] start");
    var ta = ReadAsync("A", 100);
    var tb = ReadAsync("B", 100);
    var results = await Task.WhenAll(ta, tb);
    Console.WriteLine($"[par] got {string.Join(",", results)}");
}

// --- 3. Cancellation: cooperative, via CancellationToken ---
// There's no "kill this task" primitive. The producer must check the token and
// throw OperationCanceledException (or `token.ThrowIfCancellationRequested()`).
// Convention: every async API that may block takes a `CancellationToken` LAST.
static async Task DemoCancellationAsync()
{
    using var cts = new CancellationTokenSource(millisecondsDelay: 50);
    try
    {
        await LongOpAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("[cancel] caught OperationCanceledException as expected");
    }
}

// --- 4. ValueTask: avoid allocating Task on synchronous paths ---
// SyncHotPathAsync usually returns synchronously (cache hit). Returning Task<int>
// would allocate every call. Returning ValueTask<int> allocates only when we
// genuinely need to wait.
static void DemoValueTask()
{
    var cache = new TinyCache();
    Console.WriteLine($"[vt] cached:   {cache.GetAsync("x").Result}");   // sync path, 0 alloc
    Console.WriteLine($"[vt] cached:   {cache.GetAsync("x").Result}");
}

// --- 5. Exceptions in async code ---
// An exception inside an async method completes its Task in the FAULTED state.
// `await` re-throws the original exception (not wrapped). Task.WhenAll surfaces
// the FIRST exception by default; access `.Exception` for all of them.
static async Task DemoExceptionsAsync()
{
    try { await ThrowAsync(); }
    catch (InvalidOperationException ex) { Console.WriteLine($"[ex] caught: {ex.Message}"); }
}

// --- helpers ---

static async Task<string> ReadAsync(string label, int delayMs)
{
    await Task.Delay(delayMs);
    return $"{label}!";
}

static async Task LongOpAsync(CancellationToken token)
{
    for (int i = 0; i < 100; i++)
    {
        token.ThrowIfCancellationRequested();         // canonical check point
        await Task.Delay(10, token);                  // and pass the token down
    }
}

static async Task ThrowAsync()
{
    await Task.Yield();                               // forces an actual async point
    throw new InvalidOperationException("nope");
}

class TinyCache
{
    private readonly Dictionary<string, int> _cache = new() { ["x"] = 42 };

    public ValueTask<int> GetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var v))
            return new ValueTask<int>(v);             // synchronous, no allocation
        return new ValueTask<int>(LoadAsync(key));    // genuine async path
    }

    private async Task<int> LoadAsync(string key)
    {
        await Task.Delay(10);
        var v = key.Length;
        _cache[key] = v;
        return v;
    }
}
