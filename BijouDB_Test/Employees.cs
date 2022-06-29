using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Relational<Employee, Computer> ComputerRelational;

    [Json]
    public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json]
    public Relational<Employee, Computer>.Junc Computers
    { get => ComputerRelational.To(this); set { } }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn)
        .Add(out ComputerRelational, Computer.EmployeeRelational!);
}