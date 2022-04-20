using BijouDB.DataTypes;
using BijouDB_Test;
using static BijouDB.Tables;


Employees newEmployee = new();

newEmployee.Assign();

IEmployee employeeInfo = newEmployee;

employeeInfo.Name = "TizzyT";

employeeInfo.Number = 9025021;
employeeInfo.Age = long.MaxValue;


////if (TryGet(Guid.Parse("97571a82-134b-abb9-8b3c-b72beba4bc38"), out Employees? employee))
////{
////    IEmployee employeeInfo = employee!;

////    employeeInfo.Number = 9025021;

////    Console.WriteLine(employeeInfo.Number);
////}
////else
////{
////    Console.WriteLine("Failed");
////}

//////Employees.Name.ValueExists("");