using BijouDB;
using BijouDB_Test.Tables;

Employee employee = new();
employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new() { Type = "Origin" };
computer.Employees += employee;

Console.WriteLine(employee.Json);

//Console.WriteLine(624572345.3476245m.ToJson());