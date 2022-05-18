using BijouDB;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> NumberColumn;
    public static readonly Column<@long> AgeColumn;
    public static readonly Column<@bint> PointsColumn;
    public static readonly Column<@record<Employee>> ManagerColumn;
    public static readonly Column<tuple<@int, @int, @int>> PhoneNumberColumn;
    // Relational Many-To-Many
    public static readonly Relational<Employee, Computer> ComputersRelational;

    [Json] public string Name 
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json] public int Number 
    { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    [Json] public long Age 
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    [Json] public BigInteger Points 
    { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value); }

    [Json] public Employee Manager 
    { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    [TupleObject("Area", "Exchange", "Subscriber")]
    [Json] public (int Area, int Exchange, int Subscriber) PhoneNumber
    { get => PhoneNumberColumn.Get(this); set => PhoneNumberColumn.Set(this, value); }

    // The junction between this record and computer records
    [Json] public Relational<Employee, Computer>.Junc Computers
    { get => ComputersRelational.To(this); set { } }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn, Unique: false)
        .Add(out NumberColumn, Default: () => 5555555)
        .Add(out AgeColumn, Check: value => value >= 18)
        .Add(out PointsColumn)
        .Add(out ManagerColumn)
        .Add(out PhoneNumberColumn)
        .Add(out ComputersRelational, Computer.EmployeeRelational);
}