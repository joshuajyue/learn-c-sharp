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

// J: Why would we want a struct with methods? 
// C: To get value semantics (copy on assignment, no shared mutable state) for a small
// value-like thing (e.g. a coordinate, money, id).

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

// J: What is structural equality? 
// C: Two instances of a record struct are considered equal if all their fields are equal

// J: Do normal structs have structural equality?
// C: No, normal structs do not have structural equality by default. They use the default value type equality, which is based on the memory layout of the struct.
var a = new Money(100, "USD");
var b = new Money(100, "USD");
Console.WriteLine($"record struct equals: {a == b}  ({a})");   // True

// Boxing: assigning a value type to `object` (or to a non-generic interface) wraps
// it in a heap allocation. Watch out for this in hot loops.

// J: Can you eli5
// C: Boxing is when you take a value type (like a struct) and treat it as an object.
// Since value types are stored directly, but objects are stored on the heap, the system has to
// create a new object on the heap to hold the value. This process is called "boxing". It can be
// expensive if done repeatedly in a loop, because it involves allocating memory and copying data.

// J: Can you give a high-level example of where we would use this?
// C: You might use boxing when you want to store a value type in a collection that expects objects, like an ArrayList (which is non-generic).

// J: Let's say we make an array of objects and put some boxed PointStructs in it. How do we infer the type? 
// Is there any way to automatically unbox it back to the original struct type?
// C: No, you have to explicitly cast it back to the original struct type when you unbox it.

object boxed = ps1;       // allocation happens here
PointStruct unboxed = (PointStruct)boxed;
Console.WriteLine($"unboxed: ({unboxed.X},{unboxed.Y})");

static void Mutate(PointStruct p)        { p.X = -1; }
static void MutateRef(ref PointStruct p) { p.X = -1; }

struct PointStruct(int x, int y) { public int X = x; public int Y = y; }
class  PointClass (int x, int y) { public int X = x; public int Y = y; }
record struct Money(decimal Amount, string Currency);


// Summary of learning: C# is a little bit like Java but with a much more in depth primitive type system.
// C# supports structs, which can have methods, but are value types. Especially record structs, which have 
// structural equality, which is definitely useful. We can box these value types into objects to store them 
// on the heap, but it's costly. Additionally, we can use the ref keyword to pass value types by reference to methods.
// Basically a hybrid of C and Java.