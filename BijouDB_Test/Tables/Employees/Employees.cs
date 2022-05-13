using BijouDB;
using BijouDB.Columns;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public static readonly Column<@int> NumberColumn;
    public int Number { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    public static readonly Column<@long> AgeColumn;
    public long Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    public static readonly Column<@bint> PointsColumn;
    public BigInteger Points { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value); }

    public static readonly Column<@record<Employee>.nullable> ManagerColumn;
    public Employee? Manager { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    public static readonly References<Computer, @record<Employee>.nullable> ComputerReferences;
    public Computer[] Computers => ComputerReferences.For(this);

    static Employee() => SchemaBuilder<Employee>
        .Add(out NameColumn, Unique: true)
        .Add(out NumberColumn)
        .Add(out AgeColumn)
        .Add(out PointsColumn)
        .Add(out ManagerColumn)
        .Add(out ComputerReferences, () => Computer.EmployeeColumn)
        .Build();
}
