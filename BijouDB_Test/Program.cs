using BijouDB_Test;
using static BijouDB.Tables;


//Employees newEmployee = new();
//IEmployee employeeInfo = newEmployee;
//employeeInfo.Name = "Bob the builder";
//employeeInfo.Number = 9025021;
//employeeInfo.Age = long.MaxValue;

//Console.WriteLine(employeeInfo.Name);

//@bool test = (@bool)NULL;

if (TryGet(Guid.Parse("9757011e-134b-abb9-7e3c-b72beba4bc38"), out Employees? employee))
{
    IEmployee employeeInfo = employee!;

    employeeInfo.Name = null!;
    employeeInfo.Alive = true;

    Console.WriteLine(employeeInfo.Name);
}
else
{
    Console.WriteLine("Failed");
}
