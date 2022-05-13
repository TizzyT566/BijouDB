using BijouDB;
using BijouDB_Test.Tables;

Employee test = new()
{
    Name = "Thien Huynh"
};

//Computer com1 = new()
//{
//    Employee = test,
//    Type = "Alienware"
//};

//Computer com2 = new()
//{
//    Employee = test,
//    Type = "HP"
//};

//Computer com3 = new()
//{
//    Employee = test,
//    Type = "Origin"
//};

//foreach (Computer comp in Employee.ComputerReferences.For(test))
//{
//    Console.WriteLine(comp.Type);
//}

foreach (Employee employee in Record.GetAll<Employee>())
{
    Console.WriteLine(employee.Id);

    foreach(Computer comp in employee.Computers)
    {
        Console.WriteLine($"\t{comp.Employee.Name}");
    }
}