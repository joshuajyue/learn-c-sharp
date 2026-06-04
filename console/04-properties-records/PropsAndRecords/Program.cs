// Lesson 04: Properties, records, primary constructors
//
// In Java you write getName()/setName() boilerplate (or use Lombok / records since 16).
// C# bakes this in:
//   * Auto-properties:    `public string Name { get; set; }`
//   * Init-only setter:   `public string Name { get; init; }` -- settable only in
//                         object initializer or constructor; then read-only forever.
//   * Read-only get-only: `public string Name { get; }`
//   * C# 14 `field` keyword: write a custom accessor while still letting the compiler
//                            generate the backing field. No more `_name` boilerplate.


var p = new Person { Name = "Ada", Age = 36 };
// This is NOT a constructor. Rather, this is logically the same as initializing a person and setting p.Name and p.Age with a setter
// This is the difference between read-only get-only and init-only: init-only uses this logic, while read-only get-only is through the constructor.
// Purpose: much more flexible than a constructor. Don't have to overload constructors -- can simply decide which variables you want to set.

// However, with init-only, you must follow the syntax above. You cannot do var p = new Person(); p.Name = "Ada"
p.Age = 37;                  // OK
// p.Name = "Grace";          // error: init-only setter
Console.WriteLine($"Person: {p.Name}, {p.Age}, normalized={p.NormalizedName}");

// Records: reference types with VALUE EQUALITY (compiler-generated Equals/GetHashCode
// based on all properties), plus `ToString`, deconstruction, and `with` expressions
// for non-destructive copy. Java records are similar but always immutable; C# `record`
// can have mutable properties too (though immutable is idiomatic).
var u1 = new User("ada", "ada@example.com");
var u2 = new User("ada", "ada@example.com");
Console.WriteLine($"record equality: {u1 == u2}");    // True -- structural
Console.WriteLine(u1);                                // User { Id = ada, Email = ... }

var u3 = u1 with { Email = "ada2@example.com" };      // copy, change one field
Console.WriteLine($"with-copy: {u3}  original unchanged: {u1}");

// Deconstruction (like Java record patterns):
var (id, email) = u1;
Console.WriteLine($"deconstructed: id={id}, email={email}");

// Primary constructor on a regular class -- parameters are in scope for the WHOLE
// class body. Great for DI-style classes.

// J: What is DI?
// C: DI stands for Dependency Injection. It's a design pattern where an object receives its dependencies from an external source rather than creating them itself.

var greeter = new Greeter("Hello");
Console.WriteLine(greeter.Greet("world"));

// greeting is captured as a private field that cannot be accessed from the outside
// it is only available inside the class body.

// This is the difference between primary constructors for classes and records. 

class Person
{
    public required string Name { get; init; }      // `required` forces initializer
    public int Age { get; set; }

    // C# 14: custom getter that reuses the compiler-generated backing field via `field`.
    public string NormalizedName
    {
        get => field ??= Name.Trim().ToLowerInvariant();
    }
}

record User(string Id, string Email);
// These are intialized as init-only


class Greeter(string greeting)
{
    public string Greet(string who) => $"{greeting}, {who}!";
}

/*
// ✅ Explicit - fine-tuned mutability
class Person
{
    public required string Name { get; init; }  // Immutable
    public int Age { get; set; }                // Mutable
    public string DisplayName { get; }          // Computed, read-only
        = field ??= Name.ToUpper();
}

var p1 = new Person { Name = "Ada", Age = 36 };  // Object initializer
p1.Age = 37;  // ✅ OK

// ✅ Record primary - simple immutable
record User(string Id, string Email);  // Both init-only

var u = new User("ada", "ada@example.com");
// u.Id = "bob";  // ❌ Can't change

// ✅ Class primary - DI, no properties
class GreetingService(ILogger logger, string prefix)
{
    public void Greet(string name)
    {
        logger.Log($"{prefix}, {name}");  // Use captured params
    }
}

var svc = new GreetingService(myLogger, "Hello");
// svc.prefix;  // ❌ Not accessible - private
*/

// Lesson summary:
// In C#, you have properties and primary constructors. Properties are so you don't have to define getters and setters for every field.
// Primary constructors let you store parameters in a private field.

// Records are useful because they are structurally equal. Additionally, for records, primary constructors automatically create init-only
// properties, which shortens the syntax.
