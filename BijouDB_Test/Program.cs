using BijouDB;
using BijouDB_Test.Tables;

Employee employee = new();
employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new();
computer.Employees += employee;

Console.WriteLine(computer.Json);