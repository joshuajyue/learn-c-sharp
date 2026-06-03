// Lesson 12: Span<T>, ReadOnlySpan<T>, ref struct, stackalloc
//
// Why this matters: when you read dotnet/runtime libraries (or any allocation-
// sensitive code in dotnet/extensions), you'll see Span<T> everywhere. It's the
// modern primitive for "a window over a contiguous block of memory" without
// allocating or copying. Coming from C, it's "a fat pointer (ptr + length) the
// compiler verifies you can't outlive its backing storage".
//
//   Span<T>          -- mutable window into a T[] / stackalloc'd buffer / string
//   ReadOnlySpan<T>  -- read-only window; what `string` decays to via AsSpan()
//   Memory<T>        -- heap-storable equivalent of Span<T> (Span can't live on
//                       the heap — it's a `ref struct`).
//
// Hard rules for Span<T> (enforced by the compiler):
//   * It's a `ref struct`: cannot be a field of a class, cannot be boxed,
//     cannot be captured by a lambda, cannot be in an async method's locals
//     across an `await`. (All for memory safety: the compiler must guarantee
//     the span never outlives its storage.)

// --- 1. ReadOnlySpan<char> over a string ---
// No allocation. Slicing is O(1): just a new (ptr, length) pair.
ReadOnlySpan<char> path = "C:\\Code\\learnCSharp\\file.txt".AsSpan();
var lastSlash = path.LastIndexOf('\\');
var dir = path[..lastSlash];                  // slice -- still zero allocations
var name = path[(lastSlash + 1)..];
Console.WriteLine($"dir = {dir}");            // implicit ToString on print
Console.WriteLine($"name = {name}");

// --- 2. Span<T> over a stack-allocated buffer ---
// `stackalloc` allocates on the STACK (like C's `int buf[16]`). Free, no GC.
// Only usable inside a method and only via Span<T>/ReadOnlySpan<T>.
Span<int> small = stackalloc int[8];
for (int i = 0; i < small.Length; i++) small[i] = i * i;
Console.WriteLine($"sum of squares = {SumSpan(small)}");

// --- 3. Parsing without allocations ---
// `int.Parse(ReadOnlySpan<char>)` parses directly from a span — no Substring().
// This is the BCL's everyday pattern for hot-path parsing.
ReadOnlySpan<char> csv = "10,20,30,40";
int total = 0;
foreach (var range in csv.Split(','))
    total += int.Parse(csv[range]);
Console.WriteLine($"csv total = {total}");

// --- 4. Spans cannot cross await — by design ---
// async Task BadAsync() { Span<int> s = stackalloc int[4]; await Task.Yield(); }
// Compiler error: "ref struct cannot be declared in async method".
// Reason: the async state machine would have to STORE the span on the heap.
// Workaround: use Memory<T>/byte[] across the await.

// --- 5. MemoryMarshal: reinterpret memory without copying ---
// Like C's `(int*)&someStruct`. Used by serializers, encoders, intrinsics.
// Here we view 4 bytes as one int — zero-copy.
Span<byte> bytes = stackalloc byte[] { 0x01, 0x02, 0x03, 0x04 };
int asInt = System.Runtime.InteropServices.MemoryMarshal.Read<int>(bytes);
Console.WriteLine($"bytes as int (little-endian on x64): 0x{asInt:X8}");

// --- 6. Spans into arrays ---
int[] arr = [10, 20, 30, 40, 50];
Span<int> middle = arr.AsSpan(1, 3);
middle.Fill(-1);                              // mutates the underlying array
Console.WriteLine($"arr after Fill: {string.Join(',', arr)}");   // 10,-1,-1,-1,50

static int SumSpan(ReadOnlySpan<int> s)       // accepting span lets caller pass any source
{
    int total = 0;
    foreach (var x in s) total += x;
    return total;
}
