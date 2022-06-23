using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Reference<@record<Employee>.nullable, Computer> ComputerReference;

    //[Json]
    //public Relational<Employee, Computer>.Junc Computers
    //{ get => ComputerRelational.To(this); set => ComputerRelational.To(this); }

    public IEnumerable<Computer> Computers
    { get => ComputerReference.To(this); }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn)
        .Add(out ComputerReference, () => Computer.EmployeeColumn);
}