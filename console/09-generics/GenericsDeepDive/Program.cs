// Lesson 09: Generics deep dive
//
// Java generics are ERASED at runtime: `List<String>` and `List<Integer>` are
// the same class to the JVM. You cannot do `new T()`, `T[]`, or `instanceof T`.
// C# generics are REIFIED: the runtime knows the actual type argument. You can
// do `typeof(T)`, `new T()` (with a constraint), and `T[]`. The JIT may even
// generate a SEPARATE method body per value-type T (no boxing).
//
// Result: C# generics are closer to C++ templates than to Java generics, but
// type-checked once at the definition (unlike C++'s "duck-typed at instantiation").

// --- 1. Constraints control what you can do with T ---
Console.WriteLine(Max(3, 7));                         // 7    -- uses IComparable<T>
Console.WriteLine(Max("apple", "banana"));            // banana
Console.WriteLine(NewOf<Box>().Describe());           // box with default contents

var stack = new TinyStack<int>();
stack.Push(1); stack.Push(2); stack.Push(3);
Console.WriteLine($"pop: {stack.Pop()}, count: {stack.Count}");

// --- 2. Static abstract members in interfaces (C# 11) ---
// This is the foundation of generic math. An interface can require STATIC members
// (operators, factory methods). Generic code can then call `T.Zero`, `T.Parse(...)`,
// `T + T`, etc. Java has no equivalent — you'd need a separate "strategy" object.
Console.WriteLine($"sum<int>    = {Sum<int>([1, 2, 3, 4])}");
Console.WriteLine($"sum<double> = {Sum<double>([1.5, 2.5, 3.0])}");

// --- 3. Variance: `in` (contravariant) and `out` (covariant) ---
// IEnumerable<out T> means "if Dog : Animal then IEnumerable<Dog> : IEnumerable<Animal>".
IEnumerable<string> strings = ["a", "b"];
IEnumerable<object> objects = strings;                 // covariance — allowed
Console.WriteLine($"covariant cast OK, first = {objects.First()}");

// IComparer<in T> means "an IComparer<Animal> can be used where IComparer<Dog> is expected".
IComparer<object> objCmp = Comparer<object>.Default;
IComparer<string> strCmp = (IComparer<string>)objCmp;  // contravariance (cast for demo)

// --- 4. default(T) and the `default` literal ---
// Reference types: null. Value types: zero/all-fields-default. Useful when you
// can't write `new T()` (no `new()` constraint).
Console.WriteLine($"default(int)    = {default(int)}");
Console.WriteLine($"default(string) ?? = {default(string) ?? "<null>"}");

static T Max<T>(T a, T b) where T : IComparable<T>      // classic constraint
    => a.CompareTo(b) >= 0 ? a : b;

static T NewOf<T>() where T : new() => new();          // `new()` constraint enables `new T()`

// Generic math: T must be a number that supports + and has a 0 element.
// INumber<T> chains many static-abstract interfaces (IAdditionOperators, etc.).
static T Sum<T>(IEnumerable<T> xs) where T : System.Numerics.INumber<T>
{
    var total = T.Zero;
    foreach (var x in xs) total += x;
    return total;
}

class Box
{
    public string Contents { get; init; } = "default contents";
    public string Describe() => $"box with {Contents}";
}

// Reified generics in action: TinyStack<int> stores ints inline in T[] (no boxing).
// TinyStack<string> stores object references. The JIT generates appropriate code
// for each.
class TinyStack<T>
{
    private T[] _items = new T[4];
    public int Count { get; private set; }

    public void Push(T item)
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = item;
    }

    public T Pop()
    {
        if (Count == 0) throw new InvalidOperationException("empty");
        var item = _items[--Count];
        _items[Count] = default!;     // release reference so GC can collect (no-op for value types)
        return item;
    }
}
