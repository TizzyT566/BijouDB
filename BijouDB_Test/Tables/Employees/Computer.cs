using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Column<@string> TypeColumn;

    // Relational Many-To-Many
    public static readonly Relational<Computer, Employee> EmployeeRelational;

    [Json] public string Type { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }

    // The junction between this record and employee records
    [Json] public Relational<Computer, Employee>.Junc Employees { get => EmployeeRelational.To(this); set { } }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeRelational, () => Employee.ComputersRelational)
        .Add(out TypeColumn, Check: value => value != "" && value != "Dell");
}
