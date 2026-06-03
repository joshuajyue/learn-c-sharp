// Lesson 01: Hello, C#
//
// What's different from Java here:
//   * No `public class Program { public static void main(String[] args) }`.
//     File-level code IS the entry point — this is called "top-level statements".
//   * No `import System;` at the top. `ImplicitUsings` in the .csproj pulls in
//     System, System.IO, System.Linq, System.Collections.Generic, etc. automatically.
//   * `Console.WriteLine` ≈ `System.out.println`.
//   * `args` is magically in scope (string[]) — same as Java's main parameter,
//     just not declared.

Console.WriteLine("Hello, World!");

// `var` — same idea as Java 10+ `var`. Type is inferred from the RHS at compile
// time. Still statically typed; this is NOT JavaScript `var`.
var name = args.Length > 0 ? args[0] : "stranger";

// String interpolation. The `$` prefix turns the literal into a template.
// In Java you'd write `String.format("Hi, %s!", name)` or `"Hi, " + name + "!"`.
Console.WriteLine($"Hi, {name}!");

// Verbatim string with `@` — no escape processing, newlines preserved.
// Closest Java analogue is a text block (""" ... """).
// C# also has raw string literals """...""" (covered in a later lesson).
Console.WriteLine(@"
Paths look like: C:\Code\learnCSharp
No need to double the backslashes inside @""...""
");

// Quick numeric demo — `int` here is `System.Int32`, always 32-bit signed,
// just like Java's `int`. C# has `nint`/`nuint` for native-sized ints (like C's
// `intptr_t`), and `uint`/`ulong`/`ushort`/`byte` for unsigned — Java has none.
int sum = 0;
for (int i = 1; i <= 10; i++) sum += i;
Console.WriteLine($"1 + 2 + ... + 10 = {sum}");
