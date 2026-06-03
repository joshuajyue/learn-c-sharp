namespace ReflectionExercises;

using System.Reflection;

// Lesson 14 alignment: custom attributes + reflection-based serialization.
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public static class Solution
{
    // Serialize an object as "col1=val1;col2=val2;..." by walking its PUBLIC
    // properties in DECLARATION ORDER.
    //   * If a property has [Column("X")] use "X" as the key, else the property name.
    //   * Use the property's ToString() for the value (or "" if null).
    //   * Properties WITHOUT a public getter are skipped.
    public static string Serialize(object obj) => throw new NotImplementedException();
}
