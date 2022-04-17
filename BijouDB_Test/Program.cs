using BijouDB_Test;
using static BijouDB.Table;

IEmployee employee = Load<Employees>(new Guid("41b7296d-dfba-3e72-7d31-baa8efae4f43"));

Console.WriteLine(employee.Name);
Console.WriteLine(employee.Number);
Console.WriteLine(employee.Age);
Console.WriteLine();