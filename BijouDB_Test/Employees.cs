using BijouDB;
using BijouDB.Primitives;

namespace BijouDB_Test;

public interface IEmployee
{
    public string Name { get; set; }
    public int Number { get; set; }
    public long Age { get; set; }
    public Employees Manager { get; set; }
}
public class Employees : Table, IEmployee
{
    // Name Column
    public static readonly Column<@string> Name;
    string IEmployee.Name { get => Name.Get(this); set => Name.Set(this, value); }

    // Number Column
    public static readonly Column<@int> Number;
    int IEmployee.Number { get => Number.Get(this); set => Number.Set(this, value); }

    // Age Column
    public static readonly Column<@long> Age;
    long IEmployee.Age { get => Age.Get(this); set => Age.Set(this, value); }

    // Manager Column
    public static readonly Column<table<Employees>> Manager;
    Employees IEmployee.Manager { get => Manager.Get(this); set => Manager.Set(this, value); }

    static Employees()
    {
        // Instantiate user defined columns
        new TableBuilder().
            Add(out Name).
            Add(out Number).
            Add(out Age).
            Add(out Manager);
    }
}
