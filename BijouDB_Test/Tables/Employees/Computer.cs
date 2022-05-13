using BijouDB;
using BijouDB.Columns;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public record Computer : Record
{
    public static readonly Column<@record<Employee>> EmployeeColumn;
    public Employee Employee { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    public static readonly Column<@string> TypeColumn;
    public string Type { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }

    static Computer() =>
        SchemaBuilder<Computer>
        .AddCol(out EmployeeColumn)
        .AddCol(out TypeColumn);
}
