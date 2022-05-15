﻿using BijouDB;
using BijouDB_Test.Tables;

Globals.Logging = true;

Employee test = new()
{
    Name = "Thien Huynh1"
};

Computer com1 = new()
{
    Employee = test,
    Type = "Alienware"
};

Computer com2 = new()
{
    Employee = test,
    Type = "HP"
};

Computer com3 = new()
{
    Employee = test,
    Type = "Origin"
};

//com1.Remove();

//com2.Remove();

//com3.Remove();

//test.Remove();

foreach (Computer comp in test.Computers)
{
    Console.WriteLine(comp.Type);
}

//foreach (Employee employee in Record.GetAll<Employee>())
//{
//    Console.WriteLine(employee.Name);
//    Console.WriteLine(Employee.ComputerReferences.HasRecords(employee));
//}