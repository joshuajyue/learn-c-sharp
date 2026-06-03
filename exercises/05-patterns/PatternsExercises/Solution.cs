namespace PatternsExercises;

// Lesson 05 alignment: switch expressions, type/property/positional/list patterns.
public abstract record Shape;
public record Circle(double Radius) : Shape;
public record Rectangle(double Width, double Height) : Shape;
public record Triangle(double Base, double Height) : Shape;

public static class Solution
{
    // Compute the area of the shape using a SINGLE switch expression.
    public static double Area(Shape s) => throw new NotImplementedException();

    // Classify with a switch expression covering:
    //   null              -> "null"
    //   int 0             -> "zero"
    //   int n < 0         -> "negative int"
    //   int n             -> "positive int"
    //   string ""         -> "empty string"
    //   string s          -> "string"
    //   int[] [1, 2, ..]  -> "starts 1,2"
    //   anything else     -> "other"
    public static string Classify(object? o) => throw new NotImplementedException();
}
