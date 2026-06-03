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

// Null-coalescing `??` and `??=`: provide a fallback / assign-if-null.
string display = optional ?? "(none)";
optional ??= "now set";
Console.WriteLine($"display={display}  optional={optional}");

// Null-forgiving `!`: "trust me, it's not null". Suppresses the warning. Use sparingly,
// only when you have a guarantee the compiler can't see.
string definitelyNotNull = MaybeNull()!;

// Pattern check that narrows the type for the rest of the block — after this `if`,
// the compiler knows `optional` is non-null inside.
if (optional is { } s)
{
    Console.WriteLine($"non-null length: {s.Length}");
}

// Nullable VALUE type — same syntax, different machinery (it's a struct wrapper).
int? maybeNumber = null;
Console.WriteLine($"HasValue={maybeNumber.HasValue}  Value-or-default={maybeNumber.GetValueOrDefault()}");

static string? MaybeNull() => DateTime.Now.Ticks % 2 == 0 ? "even" : null;
