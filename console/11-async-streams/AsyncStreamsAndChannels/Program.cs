// Lesson 11: IAsyncEnumerable and System.Threading.Channels
//
// `IEnumerable<T>` yields items SYNCHRONOUSLY. If producing each item involves
// I/O (database row, HTTP page, file chunk), blocking the thread per item is
// wasteful. `IAsyncEnumerable<T>` is the async version:
//
//     await foreach (var x in SomethingAsync()) { ... }
//
// You author one with `async` + `yield return`. Same generator-style coding as a
// regular iterator, but each yield can be preceded by `await`.
//
// `Channel<T>` is the standard producer/consumer queue (think Go channels or
// Java's BlockingQueue, but async-aware). Heavily used in dotnet/extensions for
// background processing, logging pipelines, hosted services.

await DemoAsyncStreamAsync();
await DemoChannelAsync();

// --- 1. IAsyncEnumerable producer + consumer ---
// The `[EnumeratorCancellation]` attribute tells the compiler to forward the
// token passed to `WithCancellation` into the iterator's `cancel` parameter.
// Idiomatic for any public async iterator.
static async Task DemoAsyncStreamAsync()
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
    try
    {
        await foreach (var page in FetchPagesAsync(maxPages: 100)
                                      .WithCancellation(cts.Token))
        {
            Console.WriteLine($"  page {page}");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  (cancelled — async stream stopped cleanly)");
    }
}

static async IAsyncEnumerable<int> FetchPagesAsync(
    int maxPages,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
{
    for (int i = 1; i <= maxPages; i++)
    {
        await Task.Delay(30, cancel);
        yield return i;
    }
}

// --- 2. Channel<T>: bounded async producer/consumer ---
// One or more producers Write; one or more consumers Read. Bounded channels
// apply backpressure: if the buffer is full, WriteAsync awaits instead of
// dropping or growing unboundedly.
static async Task DemoChannelAsync()
{
    var channel = System.Threading.Channels.Channel.CreateBounded<string>(
        new System.Threading.Channels.BoundedChannelOptions(capacity: 4)
        {
            FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
        });

    var producer = Task.Run(async () =>
    {
        for (int i = 0; i < 8; i++)
        {
            await channel.Writer.WriteAsync($"item-{i}");
            Console.WriteLine($"  produced item-{i}");
        }
        channel.Writer.Complete();   // signals "no more items" so consumer can exit
    });

    var consumer = Task.Run(async () =>
    {
        // ReadAllAsync gives you an IAsyncEnumerable<T> — completes when the
        // writer calls Complete() AND the buffer is drained.
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            await Task.Delay(20);
            Console.WriteLine($"  consumed {item}");
        }
    });

    await Task.WhenAll(producer, consumer);
}
