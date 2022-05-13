using BijouDB;
using BijouDB.Columns;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public record Employee : Record
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

    public static readonly References<Computer, @record<Employee>> ComputerReferences;
    public Computer[] Computers => ComputerReferences.For(this);

    static Employee()
    {
        var b = SchemaBuilder<Employee>
        .AddRef(out ComputerReferences, () => Computer.EmployeeColumn)
        .AddCol(out NameColumn, Unique: true)
        .AddCol(out NumberColumn)
        .AddCol(out AgeColumn)
        .AddCol(out PointsColumn)
        .AddCol(out ManagerColumn);
    }
}