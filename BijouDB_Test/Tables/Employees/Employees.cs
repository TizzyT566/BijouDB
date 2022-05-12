using BijouDB;
using BijouDB.Columns;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> NumberColumn;
    public static readonly Column<@long> AgeColumn;
    public static readonly Column<@bint> PointsColumn;
    public static readonly Column<@record<Employee>.nullable> ManagerColumn;
    public static readonly References<Employee, @record<Employee>.nullable> EmployeesColumn;

    static Employee() => new SchemaBuilder<Employee>()
        .Add(out NumberColumn)
        .Add(out AgeColumn)
        .Add(out ManagerColumn)
        .Add(out PointsColumn)
        .Add(out NameColumn)
        .Add(() => ManagerColumn, out EmployeesColumn);

    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }
    public int Number { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }
    public long Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }
    public BigInteger Points { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value!); }
    public Employee? Manager { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }
    public Employee[] Employees => EmployeesColumn.Get(this);
}