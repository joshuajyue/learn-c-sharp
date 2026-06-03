// Lesson 03: Nullable reference types (NRT)
//
// In Java, every reference can be null and the compiler doesn't help you. You annotate
// with @Nullable / @NonNull or wrap in Optional<T>.
// In C# with `<Nullable>enable</Nullable>` (see .csproj):
//   * `string`  means "must not be null". The compiler WARNS on possible nulls.
//   * `string?` means "may be null". The compiler forces you to check before use.
// Value types use the same syntax: `int?` is `Nullable<int>` (≈ Java Integer vs int).

string required = "hello";
string? optional = null;
Console.WriteLine($"required = {required}");
// optional.Length;        // <- compiler warning CS8602: dereference of possibly null  

// Null-conditional `?.` and `?[]`: short-circuit, return null if LHS is null.
int? len = optional?.Length;
Console.WriteLine($"len = {len?.ToString() ?? "<null>"}");
// J: what does ?? mean?
// C: The `??` operator is the null-coalescing operator.
// It returns the left-hand operand if it is not null; otherwise, it returns the right-hand operand.

// Null-coalescing `??` and `??=`: provide a fallback / assign-if-null.
string display = optional ?? "(none)";
optional ??= "now set";
Console.WriteLine($"display={display}  optional={optional}");

// Null-forgiving `!`: "trust me, it's not null". Suppresses the warning. Use sparingly,
// only when you have a guarantee the compiler can't see.
string definitelyNotNull = MaybeNull()!;
//int val = definitelyNotNull.Length;

// J: What would happen if MaybeNull() actually returned null here?
// C: If `MaybeNull()` returns null, then `definitelyNotNull` would be assigned a null value.
// However, since we used the null-forgiving operator `!`, the compiler will not warn us about this
// potential null assignment. If we later try to use `definitelyNotNull` without checking for null, we could encounter a `NullReferenceException`.
// Normally, this is caught by the compiler, but the `!` operator tells it to ignore the possibility of null

// Pattern check that narrows the type for the rest of the block — after this `if`,
// the compiler knows `optional` is non-null inside.

// J: Since we did optional ??= "now set" above, shouldn't the compiler already know it's non-null?
// C: The compiler does not perform flow analysis across method calls or assignments that could potentially change the value of a variable.
// In this case, even though we assigned a non-null value to `optional` using the null-coalescing assignment operator `??=`, the compiler does not track this change in its flow analysis.

// To demonstrate this, see that optional.Length still produces a warning.
// optional.Length;

if (optional is { } s)
{
    Console.WriteLine($"non-null length: {s.Length}");
}
// J: can you explain optional is { } s in more detail?
// C: The pattern `optional is { } s` is a C# pattern matching expression that checks if `optional` is not null
// and, if it is not null, assigns its value to a new variable `s` of the same type.


// Nullable VALUE type — same syntax, different machinery (it's a struct wrapper).
int? maybeNumber = null;
Console.WriteLine($"HasValue={maybeNumber.HasValue}  Value-or-default={maybeNumber.GetValueOrDefault()}");

// J: does C# have default values? i.e. if we do int x; is it 0?
// C: Fields (class/struct members): Automatically initialized to default values (0 for int, null for reference types, false for bool, etc.
// Local variables (declared inside methods): Do NOT get default values; you must explicitly initialize them before use.
// It's the same as in Java

static string? MaybeNull() => null;

// Learning Summary
// In C#, you can add a ? to a type to signify that it can be null. For instance, string cannot be null, but string? can. We can use the question mark 
// in more places: in accessing fields of a potentially null class (string?.Length), in assigning variables (nullObj ??= Obj), as an operator (nullObj ?? "this is null"). However, since 
// variable of type type? are potentially null they need safeguards for access. You can do this with the question marks, or with a pattern match (nullObj is { } obj)
// Additionally, we can use the ! for a value to claim that the value is not null. However, this will result in a NullReferenceException, since the compiler won't be able to catch
// the null.