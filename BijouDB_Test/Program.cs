using BijouDB_Test;
using static BijouDB.Tables;


//Employees newEmployee = new();

//newEmployee.Assign();

//IEmployee employeeInfo = newEmployee;

//employeeInfo.Name = "Bob the builder";

////employeeInfo.Number = 9025021;
////employeeInfo.Age = long.MaxValue;

//Console.WriteLine(employeeInfo.Name);


if (TryGet(Guid.Parse("975706fa-134b-abb9-813c-b72beba4bc38"), out Employees? employee))
{
    IEmployee employeeInfo = employee!;

    employeeInfo.Name = "TizzyT";

    Console.WriteLine(employeeInfo.Name);
}
else
{
    Console.WriteLine("Failed");
}
