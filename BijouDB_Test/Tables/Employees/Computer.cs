using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Reference<Computer, Employee> EmployeeReferences;

    public IEnumerable<Employee> Employees
    { get => EmployeeReferences.To(this); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeReferences, () => Employee.ComputerColumn);
}
