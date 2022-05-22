using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Computer : Record
{
    // Relational Many-To-Many
    public static readonly Relational<Computer, Employee> EmployeeRelational;
    public static readonly Column<@string> TypeColumn;

    [Json]
    public string Type
    { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }
        
    [Json(true)] // The junction between this record and employee records
    public Relational<Computer, Employee>.Junc Employees
    { get => EmployeeRelational.To(this); set { } }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeRelational, Employee.ComputersRelational)
        .Add(out TypeColumn, Check: value => value != "" && value != "Dell");
}
