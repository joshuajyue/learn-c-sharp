// Lesson 07: Delegates, Func/Action, and events
//
// In Java you pass behaviour around with lambdas that target a SAM interface
// (Runnable, Function<T,R>, Consumer<T>, ...). The interface IS the type.
// In C# the "type of a method signature" is a first-class concept: a `delegate`.
// A delegate is a TYPE; an instance of it holds one or more methods you can invoke.
//
// You almost never declare your own delegate type today — the BCL ships generic
// `Func<...>` (returns a value) and `Action<...>` (returns void). Use those.
//
// Events are just delegates with restricted access: only the owner can invoke /
// reassign; subscribers can only += / -=. Same role as Java listeners.

// --- 1. Func and Action (use these 95% of the time) ---
// Action            -> void(...)
// Action<int,int>   -> void(int,int)
// Func<int>         -> int()
// Func<int,int,int> -> int(int,int)        // last type param is the RETURN
Func<int, int, int> add = (a, b) => a + b;
Action<string>      log = msg => Console.WriteLine($"[log] {msg}");

log($"add(2,3) = {add(2, 3)}");

// Method group conversion: pass a method by name; the compiler builds the delegate.
Func<int, int, int> mul = Multiply;            // no lambda needed
log($"mul(4,5) = {mul(4, 5)}");

// --- 2. Custom delegate type (rarely needed, but you'll see it in old APIs) ---
// `delegate` here declares a TYPE named Reducer, not a method.
Reducer<int> sum = (acc, x) => acc + x;
log($"reduce sum = {Reduce([1, 2, 3, 4, 5], 0, sum)}");

// --- 3. Multicast: one delegate, many targets ---
// `+` (or `+=`) chains invocations. Calling the delegate calls all of them in order.
// For Func<>, the LAST return value wins (the others are discarded — easy to miss).
Action<string> pipeline = log;
pipeline += msg => Console.WriteLine($"[upper] {msg.ToUpperInvariant()}");
pipeline("hello");        // both subscribers fire

// --- 4. Events ---
// An `event` exposes a delegate field but locks down what callers can do:
//   * outside the declaring type: only `+=` and `-=`
//   * inside the declaring type: can also invoke and reassign
// This is the standard "publish/subscribe" pattern (Java listener equivalent).
var clock = new Clock();
clock.Tick += (sender, e) => Console.WriteLine($"  tick at {e.At:HH:mm:ss.fff}");
clock.Tick += (sender, e) => Console.WriteLine($"  (second handler)");
clock.RaiseOnce();
// clock.Tick = null;                       // ERROR outside the class -- that's the point
// clock.Tick(this, new TickEventArgs(...)); // ERROR -- only Clock can invoke it

// --- 5. Convention: EventHandler<TEventArgs> ---
// The BCL standard event signature is `void (object? sender, TEventArgs e)`.
// Stick to this in public APIs; tooling, docs, and consumers expect it.

// --- 6. Closures: lambdas capture variables, not just values ---
// Same as Java, but C# lambdas can capture MUTABLE locals (Java requires effectively
// final). The captured variable is "hoisted" into a compiler-generated class.
int counter = 0;
Action bump = () => counter++;
bump(); bump(); bump();
Console.WriteLine($"counter after 3 bumps = {counter}");   // 3

static int Multiply(int a, int b) => a * b;

static TAcc Reduce<T, TAcc>(IEnumerable<T> src, TAcc seed, Reducer<TAcc> step) where T : TAcc
{
    var acc = seed;
    foreach (var x in src) acc = step(acc, (TAcc)(object)x!);  // demo only; real LINQ uses Aggregate
    return acc;
}

delegate T Reducer<T>(T accumulator, T next);

class TickEventArgs(DateTime at) : EventArgs
{
    public DateTime At { get; } = at;
}

class Clock
{
    // The `event` keyword turns the delegate field into an event.
    // Subscribers do `clock.Tick += handler`; only Clock can invoke `Tick?.Invoke(...)`.
    public event EventHandler<TickEventArgs>? Tick;

    public void RaiseOnce()
    {
        // Null-conditional `?.Invoke(...)` is the canonical thread-safe way to raise:
        // it captures the field once and only invokes if there are subscribers.
        Tick?.Invoke(this, new TickEventArgs(DateTime.Now));
    }
}
