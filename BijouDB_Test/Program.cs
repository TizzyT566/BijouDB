using BijouDB_Test;
using static BijouDB.Tables;


//Employees newEmployee = new();

//newEmployee.Assign();

//newEmployee.Columns.Age = 84;

////employeeInfo.Points = System.Numerics.BigInteger.Parse("452468476496486947785434565478964794641641246475949684312613241645641684742635445194167423454315187685464216416447356471349684764346748");

////employeeInfo.Number = 9025021;
////employeeInfo.Age = long.MaxValue;

//Console.WriteLine(newEmployee.Columns.Age);


if (TryGet(Guid.Parse("97574d4a-134b-abb9-a53c-b72beba4bc38"), out Employees? employee))
{
    employee!.Remove();
    //employee!.Columns.Points = 481;
    Console.WriteLine(employee!.AsRecord.Age);
}
else
{
    Console.WriteLine("Failed");
}

//Employees.Points.IndexedValueExists(System.Numerics.BigInteger.Parse("452468476496486947785434565478964794641641246475949684312613241645641684742635445194167423454315187685464216416447356471349684764346748"), out Guid hash, out Guid value);
//Employees.Points.

