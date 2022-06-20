using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@record<Computer>> ComputerColumn;
    public static readonly Column<@string> NameColumn;

    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public Computer Computer { get => ComputerColumn.Get(this); set => ComputerColumn.Set(this, value); }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn)
        .Add(out ComputerColumn);
}