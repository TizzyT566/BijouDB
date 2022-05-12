using BijouDB;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public sealed class Employee : Schema
{
    public static readonly IndexedColumn<Employee, @string> NameColumn;
    public string Name { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    public static readonly IndexedColumn<Employee, @int> NumberColumn;
    public int Number { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    public static readonly IndexedColumn<Employee, @long> AgeColumn;
    public long Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    public static readonly IndexedColumn<Employee, @bint.nullable> PointsColumn;
    public BigInteger? Points { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value!); }

    public static readonly IndexedColumn<Employee, record<Employee>> ManagerColumn;
    public Employee Manager { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value); }

    public static readonly ReferenceColumn<Employee, Employee> EmployeesColumn;
    public IReadOnlySet<Guid> Employees => EmployeesColumn.Get(this);

    static Employee() => new ColumnBuilder<Employee>()
        .Index(out NameColumn, ColumnType.Unique)
        .Index(out NumberColumn, ColumnType.Indexed)
        .Index(out AgeColumn)
        .Index(out ManagerColumn)
        .Index(out PointsColumn)
        .Refer(out EmployeesColumn, () => ManagerColumn)
        .Remove()
}