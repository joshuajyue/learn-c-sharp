# Copilot instructions for learnCSharp

This repository is a personal learning workspace for picking up **C# on .NET 10**.
The author already knows Java (data structures, OOP, polymorphism, references) and C
(structs, unions, pointers). Tailor explanations to that background: contrast with Java
or C where it clarifies a concept, and skip basics that transfer directly (e.g. `if`/`for`
syntax, classes, inheritance fundamentals).

## Repository purpose & scope

Planned learning tracks (each lives in its own subfolder/solution as it is added):

- **console/** — small console apps for language features (LINQ, async/await, records,
  pattern matching, nullable reference types, spans, generics, etc.).
- **library/** — class library projects for practicing API design, packaging, and
  consuming code from other projects.
- **aspnet/** — ASP.NET Core experiments (minimal APIs, MVC, Razor, dependency
  injection, middleware).
- **exercises/** — focused katas / problem-set solutions.

When the user asks to start a new topic, create a new project under the appropriate
track rather than mixing topics in an existing project.

## Toolchain

- Target framework: **`net10.0`** (set `<TargetFramework>net10.0</TargetFramework>`).
- SDK-style projects only. Prefer **top-level statements** and **implicit usings** for
  new console samples unless the lesson is specifically about `Main` / `using`.
- Enable **`Nullable`** (`<Nullable>enable</Nullable>`) and **`ImplicitUsings`** by
  default — these are teaching surface area worth seeing.

### Common commands

```pwsh
# Create projects (run from the track folder, e.g. console/)
dotnet new console   -n SampleName -f net10.0
dotnet new classlib  -n SampleLib  -f net10.0
dotnet new web       -n SampleApi  -f net10.0    # ASP.NET Core minimal API
dotnet new xunit     -n SampleTests -f net10.0

# Solution wiring
dotnet new sln
dotnet sln add path\to\Project.csproj

# Build / run / test
dotnet build
dotnet run --project path\to\Project.csproj
dotnet test

# Run a single test (xUnit)
dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"
# or by display name / trait:
dotnet test --filter "DisplayName~partial name"
dotnet test --filter "Category=Smoke"

# Formatting
dotnet format
```

Use **xUnit** as the default test framework unless a lesson is explicitly about NUnit
or MSTest.

## Teaching conventions

When generating or explaining code in this repo:

- **Compare to Java/C explicitly** when a feature differs meaningfully. Examples worth
  always calling out:
  - `struct` vs `class` (value vs reference semantics — closer to C structs than Java).
  - `ref` / `out` / `in` parameters vs Java's pass-by-value-of-reference.
  - Properties (`{ get; set; }`) vs Java getters/setters.
  - `record` / `record struct` vs plain classes; value equality semantics.
  - Nullable reference types (`string?`) vs Java's `@Nullable` / `Optional`.
  - `IEnumerable<T>` + LINQ vs Java `Stream`.
  - `Task` / `async` / `await` vs Java `CompletableFuture`.
  - `using` / `IDisposable` vs Java try-with-resources.
  - Extension methods, `delegate`/`event`, pattern matching, `switch` expressions —
    no direct Java equivalent; explain from scratch.
- **Do not re-explain** OOP basics, generics fundamentals, exceptions, or collection
  semantics that map 1:1 to Java (`List<T>` ≈ `ArrayList`, `Dictionary<K,V>` ≈ `HashMap`,
  etc.). A one-line analogy is enough.
- Prefer **modern idiomatic C#**: file-scoped namespaces, target-typed `new()`,
  collection expressions (`[1, 2, 3]`), `var` when the type is obvious from the RHS,
  primary constructors where they aid clarity.
- Keep samples **small and self-contained**. One concept per file/project. Add brief
  `//` comments only where C# behaviour would surprise a Java/C developer.
- When introducing a new feature, include a short runnable `Program.cs` (or test) that
  demonstrates it — don't leave abstract snippets.

## Project layout conventions

- Each track folder (`console/`, `library/`, `aspnet/`, `exercises/`) is independent —
  no shared solution at the repo root unless the user asks for one.
- Within a track, group related lessons under a parent folder (e.g.
  `console/01-basics/`, `console/02-linq/`). Numeric prefixes keep ordering obvious.
- Test projects sit next to the code they test: `Foo/` and `Foo.Tests/`.

## Reference repos & docs

When you need an authoritative answer about how the runtime, BCL, or ASP.NET behaves,
prefer these sources over guessing:

- **dotnet/runtime** — CoreCLR, BCL, libraries source. Useful both for "how is `X`
  actually implemented" and for canonical idiomatic C# style.
- **Book of the Runtime (BotR)** — design docs living inside that repo at
  `docs/design/coreclr/botr/` (e.g. `garbage-collection.md`, `type-system.md`,
  `method-descriptor.md`). Read these when a lesson touches GC, JIT, type loading,
  threading, or interop — they are the "why" behind the runtime.
- **dotnet/aspnetcore** — for the ASP.NET track, mirror its minimal-API and DI samples.
- **Microsoft Learn / .NET API docs** — first stop for language and BCL questions,
  especially for .NET 10 features that are new and easy to misremember.

When using these as a reference, cite the file path or doc URL in any explanation so
the learner can read more.

## When unsure

Ask before scaffolding large multi-project solutions or pulling in third-party NuGet
packages — for learning, the BCL is usually enough. For ASP.NET work, prefer the
**minimal API** template first, then graduate to MVC/controllers when the lesson
calls for it.
