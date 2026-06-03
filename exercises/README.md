# exercises/

Kata-style problem sets, one per console lesson. Each is a single **xUnit** project (`net10.0`).

## How to use

```pwsh
# Run all tests in one exercise
cd exercises/01-hello/HelloExercises
dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Greet_returns_expected"

# Run every exercise from the repo root
dotnet test    # if a solution-wide test runner is configured later
```

Workflow:

1. Open `Solution.cs` — every method starts as `throw new NotImplementedException()`.
2. Open `Tests.cs` — read the `[Fact]` / `[Theory]` cases to understand the contract.
3. Implement `Solution.cs` until tests are green.
4. Re-read the matching `console/NN-*` lesson if a concept feels fuzzy.

## Exercise → lesson map

| Exercise                       | Lesson topic                                  | Notes |
|--------------------------------|-----------------------------------------------|---|
| 01-hello                       | top-level syntax, `var`, interpolation        |   |
| 02-value-vs-reference          | `struct` vs `class`, `ref` parameters         |   |
| 03-nullable                    | NRT, `?.`, `??`, no `!` cheating              |   |
| 04-properties-records          | `record`, `with`, `init`, `required`          | record equality test passes for free |
| 05-patterns                    | switch expressions, list/positional patterns  |   |
| 06-linq                        | `Where`/`Select`/`GroupBy`/`Aggregate`        |   |
| 07-delegates-events            | `Func<>` composition, events                  |   |
| 08-disposable                  | `IDisposable`, exception safety, idempotency  |   |
| 09-generics                    | constraints, `INumber<T>`, generic cache      |   |
| 10-async                       | retry with cancellation, `Task.WhenAny`       |   |
| 11-async-streams               | `IAsyncEnumerable<T>` batching                | arg-validation test passes for free |
| 12-spans                       | `ReadOnlySpan<char>`, allocation-free parsing |   |
| 13-unsafe                      | `fixed`, pointer arithmetic, `Unsafe.As`      | needs `AllowUnsafeBlocks` (set) |
| 14-attributes-reflection       | custom `Attribute` + reflection serializer    |   |
| 15-di-hosting                  | service lifetimes via `IServiceCollection`    |   |
| 16-source-generators           | `[LoggerMessage]`, `[GeneratedRegex]`         | starts as a **compile error**: add the attributes to fix it |
