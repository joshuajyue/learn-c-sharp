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

// Question: Where is args retrieved from?
// It's magically in scope as a string[] — same as Java's main parameter,
// just not declared. C#'s top-level statements are wrapped in an implicit `Main` method

// String interpolation. The `$` prefix turns the literal into a template.
// In Java you'd write `String.format("Hi, %s!", name)` or `"Hi, " + name + "!"`.
Console.WriteLine($"Hi, {name}!");
// With a number:
int x = 54;
double y = 3.14;
Console.WriteLine($"pi is approximately {y}, and x is {x}");


// Verbatim string with `@` — no escape processing, newlines preserved.
// Closest Java analogue is a text block (""" ... """).
// C# also has raw string literals """...""" (covered in a later lesson).
Console.WriteLine(@"
Paths look like: C:\Code\learnCSharp
No need to double the backslashes inside @""...""
");
// Same thing but written in traditional string literal form, with escapes:
Console.WriteLine("\nPaths look like: C:\\Code\\learnCSharp\nNo need to double the backslashes inside @\"...\"\"\n");

// Quick numeric demo — `int` here is `System.Int32`, always 32-bit signed,
// just like Java's `int`. C# has `nint`/`nuint` for native-sized ints (like C's
// `intptr_t`), and `uint`/`ulong`/`ushort`/`byte` for unsigned — Java has none.
int sum = 0;
for (int i = 1; i <= 10; i++) sum += i;
Console.WriteLine($"1 + 2 + ... + 10 = {sum}");

// Unsigned types (no equivalent in Java):
Console.WriteLine($"byte:   {byte.MinValue} to {byte.MaxValue}");     // 0 to 255
Console.WriteLine($"ushort: {ushort.MinValue} to {ushort.MaxValue}"); // 0 to 65,535
Console.WriteLine($"uint:   {uint.MinValue} to {uint.MaxValue}");     // 0 to 4,294,967,295
Console.WriteLine($"ulong:  {ulong.MinValue} to {ulong.MaxValue}");   // 0 to 18,446,744,073,709,551,615

// Native-sized integers (like C's intptr_t — size depends on platform: 32-bit or 64-bit):
Console.WriteLine($"nint:   {nint.MinValue} to {nint.MaxValue}");     // platform-dependent
Console.WriteLine($"nuint:  {nuint.MinValue} to {nuint.MaxValue}");   // platform-dependent

// Other signed types (Java has these except sbyte):
Console.WriteLine($"sbyte:  {sbyte.MinValue} to {sbyte.MaxValue}");   // -128 to 127
Console.WriteLine($"short:  {short.MinValue} to {short.MaxValue}");   // -32,768 to 32,767
Console.WriteLine($"long:   {long.MinValue} to {long.MaxValue}");     // -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807

