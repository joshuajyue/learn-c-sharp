// Lesson 16: Source generators (as a CONSUMER)
//
// A source generator is a Roslyn component that PARTICIPATES in compilation:
// it sees your source code, then emits additional C# files that the compiler
// compiles alongside yours. Output is regular, debuggable C# — no runtime
// reflection, no IL weaving. Result: the speed of hand-written code with the
// convenience of attribute-driven APIs.
//
// You'll meet three of them constantly when reading dotnet/runtime and
// dotnet/extensions:
//   1. LoggerMessage source generator  (Microsoft.Extensions.Logging)
//   2. Regex source generator          (System.Text.RegularExpressions, .NET 7+)
//   3. System.Text.Json source generator
//
// Writing one is a separate project type (`<IsRoslynComponent>true</...>`,
// reference Microsoft.CodeAnalysis.CSharp). That's a Day 2 topic — focus first
// on recognising and using the ones the BCL ships.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// --- 1. LoggerMessage source generator ---
// `[LoggerMessage]` on a PARTIAL method makes the generator emit a fast,
// allocation-free implementation. No string formatting at log time when the
// level is disabled. Read any logging code in dotnet/extensions and you'll see
// dozens of these.
ILogger logger = NullLogger.Instance;        // a no-op logger for the demo
Log.UserLoggedIn(logger, userId: 42, source: "web");

// --- 2. Regex source generator ---
// `[GeneratedRegex]` on a partial method emits a fully-specialised matcher at
// compile time — no IL interpretation, no per-call cache lookup. The runtime
// Regex(string) constructor still works; the source generator is the modern
// optimised path.
var input = "Order #1234 shipped 2026-06-03";
var match = OrderIdRegex().Match(input);
Console.WriteLine($"regex matched: '{match.Value}' (groups[1] = {match.Groups[1].Value})");

// --- 3. System.Text.Json source generator ---
// `JsonSerializerContext` + `[JsonSerializable]` emits typed (de)serializers at
// compile time. AOT-friendly (no reflection), trimming-friendly, faster than
// the reflection-based serializer.
var person = new Person("Ada", 36);
string json = JsonSerializer.Serialize(person, AppJsonContext.Default.Person);
Console.WriteLine($"json: {json}");

var roundTripped = JsonSerializer.Deserialize(json, AppJsonContext.Default.Person);
Console.WriteLine($"deserialized: {roundTripped}");

// --- Type declarations consumed by the generators ---

internal record Person(string Name, int Age);

internal static partial class Log
{
    // The compiler will emit the BODY of this method. EventId, Level, and
    // message template are baked in; placeholders {UserId}, {Source} map to
    // the parameters by name.
    [LoggerMessage(
        EventId = 1001,
        Level   = LogLevel.Information,
        Message = "User {UserId} logged in from {Source}")]
    public static partial void UserLoggedIn(ILogger logger, int userId, string source);
}

internal static partial class OrderRegex
{
    // Placeholder so we can show usage of GeneratedRegex elsewhere if needed.
}

internal partial class Program
{
    // GeneratedRegex on a partial method => compile-time generated matcher.
    [GeneratedRegex(@"Order #(\d+)", RegexOptions.Compiled)]
    private static partial Regex OrderIdRegex();
}

// JsonSerializerContext: declare every type you want fast (de)serialization for.
[JsonSerializable(typeof(Person))]
internal partial class AppJsonContext : JsonSerializerContext { }
