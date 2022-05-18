﻿using BijouDB;
using BijouDB_Test.Tables;

Employee employee = new();
employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new() { Type = "Origin" };
computer.Employees += employee;

Console.WriteLine(employee.ToJson(true, 1));