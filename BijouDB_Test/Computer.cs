using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Column<@record<Employee>.nullable> EmployeeColumn;

    [Json]
    public Employee? Employee
    { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn);
}
