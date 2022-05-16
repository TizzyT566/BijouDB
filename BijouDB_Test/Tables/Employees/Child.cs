using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public sealed class Child : Record
{
    public static readonly Column<@int> AgeColumn;
    [Json] public int Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    public static readonly Column<@blob> DataColumn;
    [Json] public byte[] Data { get => DataColumn.Get(this); set => DataColumn.Set(this, value); }

    public static readonly Column<@index<Person, @string>> ParentColumn;
    [Json, TupleObject("Record", "Index")]
    public (Person Record, string Index) Parent { get => ParentColumn.Get(this); set => ParentColumn.Set(this, value); }

    static Child() => _ = ~SchemaBuilder<Child>
        .Add(out AgeColumn)
        .Add(out DataColumn)
        .Add(out ParentColumn);
}