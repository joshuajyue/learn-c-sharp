// Lesson 08: IDisposable, using, and deterministic cleanup
//
// In Java you have try-with-resources:
//     try (var r = new FileReader(...)) { ... }   // AutoCloseable.close() runs
// In C the equivalent is "remember to call fclose() / free() yourself".
//
// C# has IDisposable.Dispose(). The `using` keyword guarantees it runs, even on
// exception. The .NET GC is non-deterministic (just like Java's), so anything
// holding an UNMANAGED resource (file handle, socket, OS lock, native pointer)
// MUST implement IDisposable so callers can release it promptly.
//
// Two syntaxes for `using`:
//   1. `using (var x = ...) { ... }`     -- block-scoped; disposes at }
//   2. `using var x = ...;`              -- C# 8+; disposes at the END of the
//                                            enclosing scope. Cleaner most of the time.

// --- 1. Block-scoped using ---
using (var w = new ScopedResource("file-A"))
{
    w.DoWork();
}   // Dispose runs HERE, even if DoWork threw.

// --- 2. Declaration `using` (lifetime = enclosing block) ---
{
    using var w = new ScopedResource("file-B");
    w.DoWork();
}   // Dispose runs HERE.

// --- 3. Exception-safety: Dispose still runs on throw ---
try
{
    using var w = new ScopedResource("file-C");
    w.DoWork();
    throw new InvalidOperationException("boom");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"caught: {ex.Message}");
}

// --- 4. Async disposal: IAsyncDisposable + `await using` ---
// Many modern resources (SqlConnection, Streams, HttpClient handlers) implement
// IAsyncDisposable so cleanup can be awaited (e.g. flushing buffers without
// blocking a thread). Use `await using` exactly like `using`.
await using (var a = new AsyncResource("db-conn"))
{
    await a.QueryAsync();
}

// --- 5. The full Dispose pattern (for types holding UNMANAGED resources) ---
// You only need this if you own a native handle directly (rare). For pure
// managed resources, just implement IDisposable and dispose your fields.
// Shown below for completeness; in real code, prefer SafeHandle to skip
// finalizers entirely.
using (var n = new NativeOwner()) { /* ... */ }

sealed class ScopedResource(string name) : IDisposable
{
    private bool _disposed;

    public void DoWork()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);    // BCL helper, .NET 7+
        Console.WriteLine($"  [{name}] working");
    }

    public void Dispose()
    {
        if (_disposed) return;        // idempotent: Dispose() may be called multiple times
        Console.WriteLine($"  [{name}] disposed");
        _disposed = true;
    }
}

sealed class AsyncResource(string name) : IAsyncDisposable
{
    public async Task QueryAsync()
    {
        await Task.Delay(10);
        Console.WriteLine($"  [{name}] queried");
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(10);          // pretend to flush
        Console.WriteLine($"  [{name}] async-disposed");
    }
}

// Full pattern with finalizer — only needed when YOU directly own a native
// handle. Modern code uses SafeHandle so the runtime cleans it up for you and
// you never need to write a finalizer.
class NativeOwner : IDisposable
{
    private IntPtr _handle = new(0xDEADBEEF);
    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);     // tell GC: no finalizer needed, already cleaned
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) { /* dispose managed fields here */ }
        if (_handle != IntPtr.Zero) { _handle = IntPtr.Zero; /* CloseHandle(_handle) */ }
        _disposed = true;
    }

    ~NativeOwner() => Dispose(disposing: false);   // finalizer as last-resort safety net
}
