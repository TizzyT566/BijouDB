using BijouDB;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    [Json] public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public static readonly Column<@int> NumberColumn;
    [Json] public int Number { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    public static readonly Column<@long> AgeColumn;
    [Json] public long Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    public static readonly Column<@bint> PointsColumn;
    [Json] public BigInteger Points { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value); }

    public static readonly Column<@record<Employee>> ManagerColumn;
    [Json] public Employee Manager { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    public static readonly Reference<Computer, @record<Employee>.nullable> ComputerReferences;
    [Json] public Computer[] Computers => ComputerReferences.For(this);

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn, Unique: false)
        .Add(out NumberColumn, Default: () => 5555555)
        .Add(out AgeColumn, Check: value => value >= 18)
        .Add(out PointsColumn)
        .Add(out ManagerColumn)
        .Add(out ComputerReferences, () => Computer.EmployeeColumn);
}