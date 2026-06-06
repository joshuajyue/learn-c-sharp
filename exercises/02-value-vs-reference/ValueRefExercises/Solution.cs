namespace ValueRefExercises;

// Lesson 02 alignment: struct vs class, ref params, value-vs-reference semantics.
public static class Solution
{
    // Swap two values in place. Caller will pass `ref` so changes are visible after.
    public static void Swap<T>(ref T a, ref T b)
    {
        T temp = a; a = b; b = temp;
    }
    // Something cool and different from Java: when we do ref T a, and a = ..., it directly modifies the caller's variable and reference.
    // In java, we'd have to access a's fields to modify it, and reassigning a would just change the local reference.

    // Increment p.X by dx, p.Y by dy, mutating the caller's PointStruct in place.
    // (PointStruct is a value type -- you'll need `ref`.)
    public static void Translate(ref PointStruct p, int dx, int dy)
    {
        p.X += dx;
        p.Y += dy;
    }
    // Return a NEW PointStruct moved by (dx, dy) without mutating the input.
    public static PointStruct Moved(PointStruct p, int dx, int dy) =>
        new PointStruct(p.X + dx, p.Y + dy);
    // lambda
}

// Don't modify the type definitions below — the tests rely on these shapes.
public struct PointStruct(int x, int y)
{
    public int X = x;
    public int Y = y;
}
