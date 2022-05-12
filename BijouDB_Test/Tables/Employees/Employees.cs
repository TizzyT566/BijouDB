using BijouDB;
using BijouDB.Columns;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@int> RndColumn;
    public static readonly IndexedColumn<Employee, @string> NameColumn;
    public static readonly IndexedColumn<Employee, @int> NumberColumn;
    public static readonly IndexedColumn<Employee, @long> AgeColumn;
    public static readonly IndexedColumn<Employee, @bint> PointsColumn;
    public static readonly IndexedColumn<Employee, @record<Employee>> ManagerColumn;
    public static readonly ReferenceColumn<Employee, @record<Employee>> EmployeesColumn;

    static Employee() => new SchemaBuilder<Employee>()
        .Column(out RndColumn)
        .Indexed(out NumberColumn)
        .Indexed(out AgeColumn)
        .Indexed(out ManagerColumn)
        .Indexed(out PointsColumn)
        .Indexed(out NameColumn)
        .Reference(() => ManagerColumn, out EmployeesColumn);

    public int Rnd { get => RndColumn.Get(this); set => RndColumn.Set(this, value); }
    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }
    public int Number { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }
    public long Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }
    public BigInteger Points { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value!); }
    public Employee Manager { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    public Employee[] Employees => EmployeesColumn.Get(this);
}