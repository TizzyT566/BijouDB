using BijouDB;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test.Tables;

public partial class Employees : Table
{
    // Columns
    public static readonly IndexedColumn<Employees, @string> NameColumn;
    public static readonly IndexedColumn<Employees, @int> NumberColumn;
    public static readonly IndexedColumn<Employees, @long> AgeColumn;
    public static readonly IndexedColumn<Employees, record<Employees>> ManagerColumn;
    public static readonly IndexedColumn<Employees, @bint.nullable> PointsColumn;
    public static readonly ReferencesColumn<Employees, Employees> EmployeeReferencesColumn;

    // Placeholders
    private string _Name = null!;
    private int _Number;
    private long _Age;
    private Employees _Manager = null!;
    private BigInteger? _Points;

    // Properties
    public string Name
    {
        get => Active ? NameColumn.Get(this) : _Name;
        set
        {
            if (Active) NameColumn.Set(this, value);
            else _Name = value;
        }
    }
    public int Number
    {
        get => Active ? NumberColumn.Get(this) : _Number;
        set
        {
            if (Active) NumberColumn.Set(this, value);
            else _Number = value;
        }
    }
    public long Age
    {
        get => Active ? AgeColumn.Get(this) : _Age;
        set
        {
            if (Active) AgeColumn.Set(this, value);
            else _Age = value;
        }
    }
    public Employees Manager
    {
        get => Active ? ManagerColumn.Get(this) : _Manager;
        set
        {
            if (Active) ManagerColumn.Set(this, value);
            else _Manager = value;
        }
    }
    public BigInteger? Points
    {
        get => Active ? PointsColumn.Get(this) : _Points;
        set
        {
            if (Active) PointsColumn.Set(this, value!);
            else _Points = value;
        }
    }
    public IReadOnlyList<Employees> EmployeeReferences => EmployeeReferencesColumn.Get(this);

    // Remover, to delete records
    private static readonly Func<Employees, bool> _remove;

    public bool Remove() => _remove(this);

    static Employees()
    {
        // Instantiate user defined columns
        new ColumnBuilder()
            .Indexs(out NameColumn, ColumnType.Unique)
            .Indexs(out NumberColumn, ColumnType.Indexed)
            .Indexs(out AgeColumn)
            .Indexs(out ManagerColumn)
            .Indexs(out PointsColumn)
            .Refers(out EmployeeReferencesColumn, () => ManagerColumn)
            .Remove(out _remove);
    }

    // Required default constructor
    public Employees() { }
}