using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

class Child : Record
{
    public static readonly Column<@int> AgeColumn;
    public int Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    public static readonly Column<@index<Person, @string>> ParentColumn;
    public (Person Record, string Index) Parent { get => ParentColumn.Get(this); set => ParentColumn.Set(this, value); }

    static Child() => _ = ~SchemaBuilder<Child>
        .Add(out AgeColumn)
        .Add(out ParentColumn);
}