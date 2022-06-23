using BijouDB_Test.Tables;
using BijouDB;

Employee employee = new();

_ = new Computer()
{
    Employee = employee
};

_ = new Computer()
{
    Employee = employee
};

Console.WriteLine(employee.Computers.ToJson(0));