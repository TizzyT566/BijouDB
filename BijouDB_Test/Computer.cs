
//namespace BijouDB_Test.Tables;

//public sealed class Computer : Record
//{
//    public static readonly Column<@string> NameColumn;
//    public static readonly Relational<Computer, Employee> EmployeeRelational;

//    [Json]
//    public string Name
//    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

//    [Json]
//    public Relational<Computer, Employee>.Junc Employees
//    { get => EmployeeRelational.To(this); set { } }

//    static Computer() => _ = ~SchemaBuilder<Computer>
//        .Add(out NameColumn)
//        .Add(out EmployeeRelational, Employee.ComputerRelational);
//}
