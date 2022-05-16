using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    public static readonly Column<@record<Employee>.nullable> EmployeeColumn;
    public static readonly Column<@string> TypeColumn;

    // Marked with 'JsonAttribute'
    [Json] public Employee? Employee { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    // Marked with 'JsonAttribute'
    [Json] public string Type { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn)
        .Add(out TypeColumn, Check: value => value != "" && value != "Dell");
}
