using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public sealed class Person : Record
{
    public static readonly Column<@string> NameColumn;
    [Json] public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public static readonly Column<tuple<@int, @int, @int>> PhoneNumberColumn;
    [Json] public (int, int, int) PhoneNumber { get => PhoneNumberColumn.Get(this); set => PhoneNumberColumn.Set(this, value); }

    public static readonly Reference<Child, @index<Person, @string>> ChildReferences;
    public Indexer<Child, @string> Children => new(i => ChildReferences.For((this, i)));

    static Person() => _ = ~SchemaBuilder<Person>
        .Add(out NameColumn)
        .Add(out PhoneNumberColumn)
        .Add(out ChildReferences, () => Child.ParentColumn);
}
