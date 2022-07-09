using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;

    [Json] public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    static Employee() => _ = ~SchemaBuilder<Employee>.Add(out NameColumn);

    public Employee() { throw new NotSupportedException(); }

    public Employee(string name)
    {
        Name = name;
    }
}