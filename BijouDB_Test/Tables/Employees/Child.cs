using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

class Child : Record
{
    public static readonly Column<@string> NameColumn;
    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public static readonly Column<@index<Person, @int>> ParentColumn;
    public (Person Record, int Index) Parent { get => ParentColumn.Get(this); set => ParentColumn.Set(this, value); }

    static Child() => _ = ~SchemaBuilder<Child>
        .Add(out NameColumn)
        .Add(out ParentColumn);
}