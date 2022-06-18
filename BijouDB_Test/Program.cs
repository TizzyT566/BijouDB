using BijouDB;
using BijouDB_Test.Tables;

Employee employee = new()
{
    Name = "DanTran"
};

Employee[] employees = Record.GetAll<Employee>();
foreach (Employee e in employees)
    Console.WriteLine(e.Json);

Console.WriteLine(employee.ToJson(0));