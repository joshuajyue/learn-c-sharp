namespace UnsafeExercises;

// Lesson 13 alignment: unsafe, fixed, pointer arithmetic.
// The csproj enables <AllowUnsafeBlocks>. Implementations MUST use pointers
// (the test suite intentionally has tiny inputs so safe code would work — the
// learning goal is the pointer mechanics, not perf).
public static unsafe class Solution
{
    // Reverse the array IN PLACE using two pointer walks (left++, right--).
    // Use `fixed (int* p = arr)` to pin the array first.
    public static void ReverseInPlace(int[] arr) =>
        throw new NotImplementedException();

    // Sum the array using ONLY pointer arithmetic inside a `fixed` block.
    public static long SumWithPointer(int[] arr) =>
        throw new NotImplementedException();

    // Reinterpret a double's bits as a long without using BitConverter.
    // Use Unsafe.As<double, long>(ref d) -- shown in lesson 13.
    public static long DoubleBits(double d) =>
        throw new NotImplementedException();
}
