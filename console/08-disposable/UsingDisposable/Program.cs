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
// What is the enclosing scope?
//  * If declared in a method, it's the method body (or nearest enclosing block).

// --- 1. Block-scoped using ---
using (var w = new ScopedResource("file-A"))
{
    w.DoWork();
}   // Dispose runs HERE, even if DoWork threw.
// Whats the control flow in the case of an exception? What comes first, the exception or the dispose?
// C: The control flow will first execute Dispose before the exception is propagated to the caller.
// So what happens if we have more code after the exception?
// C: it  will 1. call Dispose, 2. propagate the exception, and 3. exit the block. 
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
// Where does dispose run? 
// C: Dispose runs in the `finally` block that the compiler generates for the `using` statement. 
// So after the exception is thrown, the control flow will jump to the 'catch' block, but C# Disposes the resource before that
// Because it knows that try is done
// Ok, what if we used a label to jump out of the block? Will dispose still run?
// C: If you use a label to jump out of the block, the Dispose method will still be called
// What if we had another label that jumped back into the block? Like, we jump out of the block that the using is in, and then we jump back into it.
// C: If you jump back into the block that contains the `using` statement, the Dispose method will still be called when the control flow exits the block
// So, we get an exception?
// C: Yes, if you jump back into the block that contains the `using` statement, you would likely encounter an exception 

// 
// --- 4. Async disposal: IAsyncDisposable + `await using` ---
// Many modern resources (SqlConnection, Streams, HttpClient handlers) implement
// IAsyncDisposable so cleanup can be awaited (e.g. flushing buffers without
// blocking a thread). Use `await using` exactly like `using`.

// Why is this part taught before async 

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
// How does this actually dispose the resource?
// Ok so it calls Dispose, but i dont see any actual garbage collection happening here
// C: The `Dispose` method is responsible for releasing unmanaged resources that the object may be holding onto
// Yes. but in this specific implementation i dont see that. Is this overloaded?
// C: In this specific implementation of `ScopedResource`, the `Dispose` method does not actually release any unmanaged resources
// How would we actually release resources in this pattern?
// C: To actually release resources in this pattern, you would typically include code in the `Dispose` method that releases any unmanaged resources that the object is holding onto.
// For example, file.Close()

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

//Lesson summary: i skipped the async stuff for later but we can use using on a class that implements IDisposable or IAsyncDisposable.
// Either at the end of the defined block or at the end of the enclosing block, the Dispose method will be called, even if there is an exception.