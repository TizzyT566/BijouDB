using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Relational<Computer, Employee> EmployeeRelational;

    [Json]
    public Relational<Computer, Employee>.Junc Employees
    { get => EmployeeRelational.To(this); set => EmployeeRelational.To(this); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeRelational, Employee.ComputerRelational);
}
