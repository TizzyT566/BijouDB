using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Relational<Employee, Computer> ComputerRelational;

    [Json]
    public Relational<Employee, Computer>.Junc Computers
    { get => ComputerRelational.To(this); set => ComputerRelational.To(this); }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out ComputerRelational, Computer.EmployeeRelational);
}