namespace LinqExercises;

// Lesson 06 alignment: LINQ Where/Select/GroupBy/Aggregate.
public record Employee(string Name, string Dept, decimal Salary);

public static class Solution
{
    // Total salary of all employees.
    public static decimal TotalPayroll(IEnumerable<Employee> staff) =>
        throw new NotImplementedException();

    // Highest-paid employee per department, as a Dictionary<dept, employee>.
    public static IDictionary<string, Employee> TopByDept(IEnumerable<Employee> staff) =>
        throw new NotImplementedException();

    // Names sorted descending by salary, then ascending by name for ties.
    public static IReadOnlyList<string> NamesSortedBySalaryDesc(IEnumerable<Employee> staff) =>
        throw new NotImplementedException();

    // Average salary per department.
    public static IDictionary<string, decimal> AverageByDept(IEnumerable<Employee> staff) =>
        throw new NotImplementedException();
}
