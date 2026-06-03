namespace SourceGenExercises;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

// Lesson 16 alignment: USE the LoggerMessage source generator and the
// GeneratedRegex source generator. There's no algorithm to implement — the
// exercise is to write the partial declarations correctly so the generated
// code compiles AND the tests verify it does the right thing.
public static partial class Solution
{
    // TODO: add [LoggerMessage] above this partial method so the generator
    // emits a body. Use EventId = 42, Level = Information, message template:
    //   "User {UserId} did {Action}"
    public static partial void LogUserAction(ILogger logger, int userId, string action);

    // TODO: add [GeneratedRegex] above this partial method so the generator
    // emits a Regex matcher. Pattern: ^([A-Z]{3})-(\d+)$
    // (e.g. "ABC-123" matches; group 1 = "ABC", group 2 = "123".)
    public static partial Regex OrderIdRegex();
}
