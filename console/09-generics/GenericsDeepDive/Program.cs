// Lesson 09: Generics deep dive
//
// Java generics are ERASED at runtime: `List<String>` and `List<Integer>` are
// the same class to the JVM. You cannot do `new T()`, `T[]`, or `instanceof T`.

// 

// C# generics are REIFIED: the runtime knows the actual type argument. You can
// do `typeof(T)`, `new T()` (with a constraint), and `T[]`. The JIT may even
// generate a SEPARATE method body per value-type T (no boxing).
//
// Result: C# generics are closer to C++ templates than to Java generics, but
// type-checked once at the definition (unlike C++'s "duck-typed at instantiation").

// --- 1. Constraints control what you can do with T ---
using System.Diagnostics.CodeAnalysis;

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
// Does java also have an equivalent of the INumber interface?
// C#: No, Java does not have a built-in equivalent of the `INumber` interface 
// Wow thats pretty cool. So we have an interface that defines numbers and defines the number system's 'Zero'
// Yes, it is pretty cool
// This interface has operators too? How does that work?
// C#: The `INumber<T>` interface in C# includes static abstract members that define the behavior of numbers, including operators.
// How does that define addition?
// C#: The `INumber<T>` interface defines addition through a static abstract operator method. For example, it might include a declaration like `public static abstract T operator +(T left, T right);`

// --- 3. Variance: `in` (contravariant) and `out` (covariant) ---
// IEnumerable<out T> means "if Dog : Animal then IEnumerable<Dog> : IEnumerable<Animal>".
// Explain in plain english: if dog is a subtype of animal, then an IEnumerable of dogs is a subtype of an IEnumerable of animals.
IEnumerable<string> strings = ["a", "b"];
IEnumerable<object> objects = strings;                 // covariance — allowed
Console.WriteLine($"covariant cast OK, first = {objects.First()}");

// IComparer<in T> means "an IComparer<Animal> can be used where IComparer<Dog> is expected".
// In plain english, since dog is a subtype of animal, we can compare dogs with a comparer of animals
IComparer<object> objCmp = Comparer<object>.Default;
IComparer<string> strCmp = (IComparer<string>)objCmp;  // contravariance (cast for demo)

// --- 4. default(T) and the `default` literal ---
// Reference types: null. Value types: zero/all-fields-default. Useful when you
// can't write `new T()` (no `new()` constraint).
Console.WriteLine($"default(int)    = {default(int)}");
Console.WriteLine($"default(string) ?? = {default(string) ?? "<null>"}");
// Why do we do the ?? "<null>" here? Just to show its null? We already know that default(string) is null, so this is just to show it in the output?
// C#: Ye

static T Max<T>(T a, T b) where T : IComparable<T>      // classic constraint
    => a.CompareTo(b) >= 0 ? a : b;

static T NewOf<T>() where T : new() => new();          // `new()` constraint enables `new T()`

// I get the where keyword in the first one -- we're checking if T implements IComparable. But what is going on with the second one?
// C#: `where T : new()` means "T must have a public parameterless constructor".
// Thats pretty crazy, can we also do T : method() or something like that?
// C#: No, you cannot use `T : method()` as a constraint in C#. 
// Can we do T : new(int)?
// C#: No, you cannot use `T : new(int)` as a constraint in C#. The `new()` constraint only allows you to specify that the type must have a public parameterless constructor.
// Seems kind of limited

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

// Pretty cool lesson. Basically, in C#, generics are much more powerful because we can do logic over them with runtime code. In Java, let's say you 
// define a class with a generic. In your methods, you cannot do anything with the generic type because it's erased at runtime.
// But here, you can. Let's say, in your class, you want to compare generics. You can write a method that runs if the type implements comparable,
// and then use the T's compareTo method. you can also write a method that creates a new instance of T.
// In java, to do the same, you would have to pass in a Class<T> object

// Additionally, in C#, you can define static abstract members in interfaces, which allows you to write generic code that can call static methods
// For example, for number systems that implement the INumber interface, we can write generic code that references the type's Zero, performs addition, etc.

// The concept of variance was also introduced, which allows for more flexible type relationships that extend beyond just the type.
// For instance, a list of dogs can be treated as a list of animals and a comparer of animals can be used where a comparer of dogs is expected
// Java would not allow this without explicit casting.