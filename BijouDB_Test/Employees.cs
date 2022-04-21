using BijouDB;
using BijouDB.DataTypes;
using System.Numerics;

namespace BijouDB_Test;

public interface IEmployee
{
    public string Name { get; set; }
    public int Number { get; set; }
    public long Age { get; set; }
    public Employees Manager { get; set; }
    public BigInteger? Points { get; set; }
}
public class Employees : Tables, IEmployee, ITable<IEmployee>
{
    public IEmployee Columns => this;

    // Name Column
    public static readonly Column<Employees, @string> Name;
    string IEmployee.Name { get => Name.Get(this); set => Name.Set(this, value!); }

    // Number Column
    public static readonly Column<Employees, @int> Number;
    int IEmployee.Number { get => Number.Get(this); set => Number.Set(this, value); }

    // Age Column
    public static readonly Column<Employees, @long> Age;
    long IEmployee.Age { get => Age.Get(this); set => Age.Set(this, value); }

    // Manager Column
    public static readonly Column<Employees, record<Employees>> Manager;
    Employees IEmployee.Manager { get => Manager.Get(this); set => Manager.Set(this, value); }

    // Alive Column
    public static readonly Column<Employees, @bint.nullable> Points;
    BigInteger? IEmployee.Points { get => Points.Get(this); set => Points.Set(this, value!); }

    static Employees()
    {
        // Instantiate user defined columns
        _ = new ColumnBuilder()
            .Add(out Name, ColumnType.Unique)
            .Add(out Number, ColumnType.Indexed)
            .Add(out Age, ColumnType.Protected)
            .Add(out Manager)
            .Add(out Points);
    }

    // Required default constructor
    public Employees() { }

    // Adds an entry with the following employee values
    public Employees(string name)
    {

    }
}