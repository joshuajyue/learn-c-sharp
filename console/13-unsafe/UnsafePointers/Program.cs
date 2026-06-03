// Lesson 13: unsafe, fixed, pointers
//
// You already know C pointers, so this lesson is mostly "how do I write that in
// C# when I have to". You SHOULD reach for Span<T>/MemoryMarshal first — they
// give you the same perf without giving up the GC's safety net. `unsafe` is for
// real interop, custom marshalling, or hand-tuned hot paths in the BCL/JIT.
//
// Two ingredients:
//   * `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in the csproj (see this project).
//   * `unsafe` keyword on the method/block/declaration.
//
// Pinning: by default the GC moves objects around to compact the heap. Taking a
// raw pointer to one would be a use-after-move bug. `fixed` PINS the object for
// the duration of the block; the GC promises not to move it.

unsafe
{
    // --- 1. Pointer to a local (stack) ---
    int x = 42;
    int* p = &x;
    Console.WriteLine($"*p = {*p}   p = 0x{(long)p:X}");
    *p = 99;
    Console.WriteLine($"x after *p = 99: {x}");

    // --- 2. Pointer arithmetic over a managed array (must `fixed`) ---
    int[] arr = [10, 20, 30, 40, 50];
    fixed (int* start = arr)                    // pins arr; start points to arr[0]
    {
        int* end = start + arr.Length;
        long sum = 0;
        for (int* it = start; it < end; it++) sum += *it;
        Console.WriteLine($"sum via pointer walk = {sum}");
    }                                            // arr UNPINNED here

    // --- 3. sizeof, stackalloc, and raw byte iteration ---
    Console.WriteLine($"sizeof(double) = {sizeof(double)}");
    byte* buf = stackalloc byte[8];
    for (int i = 0; i < 8; i++) buf[i] = (byte)(i * 16);
    Console.Write("buf: ");
    for (int i = 0; i < 8; i++) Console.Write($"{buf[i]:X2} ");
    Console.WriteLine();

    // --- 4. Function pointers (C# 9, like C's function pointers) ---
    // Cheaper than a delegate (no allocation, no invocation list). Used in the
    // runtime's hot paths and in P/Invoke marshalling.
    delegate*<int, int, int> addPtr = &AddInts;
    Console.WriteLine($"fn-ptr add(2,3) = {addPtr(2, 3)}");
}

// --- 5. Safer alternative: Unsafe.As / MemoryMarshal ---
// These give you "reinterpret cast" without the `unsafe` keyword by relying on
// compiler intrinsics. Prefer them when possible.
double d = 1.5;
long bits = System.Runtime.CompilerServices.Unsafe.As<double, long>(ref d);
Console.WriteLine($"1.5 as bits = 0x{bits:X16}");

// `static` because top-level statements can take its address with `&AddInts`.
static int AddInts(int a, int b) => a + b;
