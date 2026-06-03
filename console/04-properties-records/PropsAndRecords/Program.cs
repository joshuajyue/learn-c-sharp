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
var greeter = new Greeter("Hello");
Console.WriteLine(greeter.Greet("world"));

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

class Greeter(string greeting)
{
    public string Greet(string who) => $"{greeting}, {who}!";
}
