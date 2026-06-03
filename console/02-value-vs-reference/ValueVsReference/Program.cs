// Lesson 02: Value types vs reference types
//
// Java mental model: primitives (int, double, ...) are values; everything else is a
// reference to a heap object.
// C mental model:    everything is a value; you reach out to other memory via pointers.
//
// C# sits between them:
//   * `struct` -> VALUE type. Lives inline (stack local, array slot, or embedded in
//     another object). Assignment COPIES the bits. Closest to a C struct.
//   * `class`  -> REFERENCE type. Lives on the heap; variables hold a reference.
//     Same as a Java object.
//
// Crucial: any TYPE you declare with `struct` is a value type, even if it has methods.

PointStruct ps1 = new(1, 2);
PointStruct ps2 = ps1;      // <-- COPY. Independent value.
ps2.X = 99;
Console.WriteLine($"struct: ps1=({ps1.X},{ps1.Y})  ps2=({ps2.X},{ps2.Y})");
// -> ps1=(1,2)  ps2=(99,2)   (Java intuition would say both change — it doesn't.)

PointClass pc1 = new(1, 2);
PointClass pc2 = pc1;       // <-- COPY of the reference. Same object.
pc2.X = 99;
Console.WriteLine($"class : pc1=({pc1.X},{pc1.Y})  pc2=({pc2.X},{pc2.Y})");
// -> pc1=(99,2) pc2=(99,2)   (Same as Java.)

// Passing to a method: by default arguments are passed by value. For a struct that
// means a COPY is passed; mutating the parameter doesn't affect the caller's struct.
// To mutate the caller's value, use `ref`.
Mutate(ps1);              Console.WriteLine($"after Mutate(ps1): ({ps1.X},{ps1.Y})");
MutateRef(ref ps1);       Console.WriteLine($"after MutateRef:   ({ps1.X},{ps1.Y})");

// `record struct` gives you value semantics PLUS structural equality & ToString —
// great for small immutable value-like things (coordinates, money, ids).
var a = new Money(100, "USD");
var b = new Money(100, "USD");
Console.WriteLine($"record struct equals: {a == b}  ({a})");   // True

// Boxing: assigning a value type to `object` (or to a non-generic interface) wraps
// it in a heap allocation. Watch out for this in hot loops.
object boxed = ps1;       // allocation happens here
PointStruct unboxed = (PointStruct)boxed;
Console.WriteLine($"unboxed: ({unboxed.X},{unboxed.Y})");

static void Mutate(PointStruct p)        { p.X = -1; }
static void MutateRef(ref PointStruct p) { p.X = -1; }

struct PointStruct(int x, int y) { public int X = x; public int Y = y; }
class  PointClass (int x, int y) { public int X = x; public int Y = y; }
record struct Money(decimal Amount, string Currency);
