using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Column<@record<Employee>.nullable> EmployeeColumn;

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn);

    [Json]
    public Employee? Employee
    { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    public Computer()
    {
        Employee = default;
    }
}
