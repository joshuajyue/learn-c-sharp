namespace PropsRecordsExercises;

// Lesson 04 alignment: properties (init/required), records, with-expressions.
public static class Solution
{
    // Build a Money instance. Implement Money below.
    public static Money MakeMoney(decimal amount, string currency) =>
        throw new NotImplementedException();

    // Use the `with` expression to return a copy of m with a different currency.
    public static Money Convert(Money m, string newCurrency) =>
        throw new NotImplementedException();

    // Add two Money values. Throw InvalidOperationException if currencies differ.
    public static Money Add(Money a, Money b) => throw new NotImplementedException();
}

// TODO: Make Money a record so == compares by value and `with` works.
// Money(Amount, Currency). Both properties are positional.
public record Money(decimal Amount, string Currency);
