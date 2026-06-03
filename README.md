# learnCSharp

A personal learning workspace for picking up **C# on .NET 10**, coming from a background in **Java** (data structures, OOP, polymorphism) and **C** (structs, unions, pointers).

## Repository Structure

This repo is organized into learning tracks, each in its own subfolder:

- **`console/`** — Console apps for exploring language features:
  - LINQ, async/await, records, pattern matching
  - Nullable reference types, spans, generics
  - Value vs. reference semantics, boxing, memory layout

- **`library/`** — Class library projects for practicing:
  - API design, packaging, project references
  - Building and consuming reusable code

- **`aspnet/`** — ASP.NET Core experiments:
  - Minimal APIs, MVC, Razor
  - Dependency injection, middleware

- **`exercises/`** — Focused katas and problem-set solutions

Each subfolder is independent — no shared solution at the root. Within each track, related lessons are grouped under numbered parent folders (e.g., `console/01-basics/`, `console/02-linq/`).

## Toolchain

- **Target Framework:** `net10.0`
- **SDK-style projects** with top-level statements and implicit usings (unless the lesson is specifically about `Main` / `using`)
- **Nullable reference types** enabled by default
- **Test Framework:** xUnit (unless a lesson explicitly covers NUnit or MSTest)

### Common Commands

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

# Formatting
dotnet format
```

## Learning Approach

- **Small, self-contained samples** — one concept per file/project
- **Modern idiomatic C#** — file-scoped namespaces, target-typed `new()`, collection expressions, `var`, primary constructors
- **Comparisons to Java/C** where C# differs meaningfully (e.g., `struct` vs `class`, `ref`/`out`, properties, records, LINQ vs `Stream`, `Task`/`async`/`await`, extension methods)
- **Runnable examples** — every feature comes with a short `Program.cs` or test that demonstrates it

## Reference Sources

When learning about runtime, BCL, or ASP.NET behavior:

- **[dotnet/runtime](https://github.com/dotnet/runtime)** — CoreCLR, BCL, libraries source
- **[Book of the Runtime (BotR)](https://github.com/dotnet/runtime/tree/main/docs/design/coreclr/botr)** — design docs on GC, JIT, type loading, threading, interop
- **[dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)** — for ASP.NET Core samples
- **[Microsoft Learn](https://learn.microsoft.com/dotnet/)** — API documentation and guides

## License

This is a personal learning repository. Feel free to use any code here for your own learning purposes.
