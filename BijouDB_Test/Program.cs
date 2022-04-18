using BijouDB_Test;
using System.Diagnostics;
using static BijouDB.Tables;


//Employees newEmployee = new();
//IEmployee employeeInfo = newEmployee;
//employeeInfo.Name = "Bob the builder";

//Console.WriteLine(employeeInfo.Name);


if (TryGet(Guid.Parse("9756ed96-134b-abb9-743c-b72beba4bc38"), out Employees? employee))
{
    IEmployee employeeInfo = employee!;
    //long start = Stopwatch.GetTimestamp();
    //Console.WriteLine(employeeInfo.Name);
    employeeInfo.Name = "TizzyT";
    //start = Stopwatch.GetTimestamp() - start;
    //Console.WriteLine(new TimeSpan(start).TotalMilliseconds);
}
else
{
    Console.WriteLine("Failed");
}