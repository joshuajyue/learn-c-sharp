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
Func<int, int, int, int, int, int, int, int> test = (a, b, c, d, e, f, g) => a + b + c + d + e + f + g;  // up to 16 params)
// This is the same as implementing an interface with a single method in Java, either void or returning the final type.
// J: what is a first class concept?
// C: A first-class concept (or first-class citizen) in programming languages refers to entities that can be treated as values.
// J: can we manipulate the func add?
// C: Yes, you can manipulate the `Func` delegate instance `add` just like any other object in C#.
// J: So lets say we did the method Hello(Func<int, int, int> operation) { ... } and we passed in add, could we then call operation(2, 3) inside the method?
// C: Yes, if you have a method like `Hello(Func<int, int, int> operation)`, you can pass the `add` delegate to it. Inside the `Hello` method, you can then call `operation(2, 3)`

log($"add(2,3) = {add(2, 3)}");

// Method group conversion: pass a method by name; the compiler builds the delegate. Does it have to be static? No, but it must be compatible with the delegate type.
// What happens if we make Multiply non static
// C: If you make the `Multiply` method non-static, you would need an instance of the class that contains the `Multiply` method to create a delegate for it.
// J: so something like Func<...> mul = m.Multiply?
// C: Yes
Func<int, int, int> mul = Multiply;            // no lambda needed
log($"mul(4,5) = {mul(4, 5)}");
// --- 2. Custom delegate type (rarely needed, but you'll see it in old APIs) ---
// `delegate` here declares a TYPE named Reducer, not a method.
Reducer<int> sum = (acc, x) => acc + x;
log($"reduce sum = {Reduce([1, 2, 3, 4, 5], 0, sum)}");
// Weird 
// How does the syntax here work?
// line 1: we declare a custom delegate called Reducer that takes two parameters of type T and returns a value of type T.
// then, we define a Reducer instance sum, which is a lambda that takes two integers and adds them
// line 2: we call the Reduce method, passing in an array of integers, a seed 0, and the sum delegate.

// Why wouldn't we just use Func<int, int, int> for sum instead of declaring a custom delegate type?
// C: You could use `Func<int, int, int>` for `sum` instead of declaring a custom delegate type. The reason to declare a custom delegate type like `Reducer<T>` is often for clarity 

// --- 3. Multicast: one delegate, many targets ---
// `+` (or `+=`) chains invocations. Calling the delegate calls all of them in order.
// For Func<>, the LAST return value wins (the others are discarded — easy to miss).
Action<string> pipeline = log;
pipeline += msg => Console.WriteLine($"[upper] {msg.ToUpperInvariant()}");
pipeline("hello");        // both subscribers fire
// How does the syntax here work? 
// line 1: we create a new Action<string> delegate called pipeline and assign it to log, which we defined earlier. So, pipeline now does the same thing as log
// line 2: we add another subscriber to the pipeline delegate using the += operator. This means that when we invoke pipeline, it will now call both the original log delegate and the new lambda 
// Can we do the same thing like this?
// Action<string> test =  msg => Console.WriteLine("hi") + msg2 => Console.WriteLine("hi2"); // C: No, you cannot use the `+` operator to combine two lambda expressions directly like that.

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
// Is EventHandler a custom delegate type or a built in one?
// C: `EventHandler<TEventArgs>` is a built-in delegate type in the .NET Framework
// What is special about EventHandler<TEventArgs>?
// It has a specific signature of void (object? sender, TEventArgs e). 


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
// Point of this: lambdas are just like any other code.

static int Multiply(int a, int b) => a * b;

static TAcc Reduce<T, TAcc>(IEnumerable<T> src, TAcc seed, Reducer<TAcc> step) where T : TAcc
{
    var acc = seed;
    foreach (var x in src) acc = step(acc, (TAcc)(object)x!);  // demo only; real LINQ uses Aggregate
    return acc;
}
// The reduce method takes a sequence of T, an initial seed of type TAcc, and a step function of type Reducer.

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
// What is the method signature of invoke?
// C: The method signature of `Invoke` for an `EventHandler<TEventArgs>` delegate is:
// void Invoke(object? sender, TEventArgs e)
// Why does this specifically need a sender? Isn't it already tied to this object's Tick?
// C: The `sender` parameter in the `Invoke` method of an event handler is a convention in .NET to provide context about the source of the event.
// So what if I put null instead of this for sender?
// C: If you put `null` instead of `this` for the `sender` parameter when invoking an event, it would mean that there is no specific source associated with the event
// But the message would still be the same?
// C: Yes, the message or the event data would still be the same, as it is passed through the `e` parameter. 

// So many moving parts. So let me get this right: when we create our clock instance, we create a delegate inside it called Tick, which is an instnace of EventHandler.
// Then, above, we add our messages, which take in e and sender and Console.WriteLine something. Specifically, e.At
// Then, we call RaiseOnce, which called Tick?.Invoke. This is how the event handler is invoked. Our e here is an instance of TickEventArgs, which is constructed with At = DateTime.Now.
// So in the subscriber's message, e.At points to the DateTime.Now that was created in RaiseOnce.
