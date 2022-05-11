using BijouDB;
using BijouDB.DataTypes;
using BijouDB_Test;
using BijouDB_Test.Tables;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static BijouDB.Table;


//Employees newEmployee = new();

////newEmployee.Assign();

//newEmployee.Name = "Test";

////employeeInfo.Points = System.Numerics.BigInteger.Parse("452468476496486947785434565478964794641641246475949684312613241645641684742635445194167423454315187685464216416447356471349684764346748");

////employeeInfo.Number = 9025021;
////employeeInfo.Age = long.MaxValue;

//Console.WriteLine(newEmployee.Name);

//Employees.Insert("test", 4, 4, new(), BigInteger.Parse("643"));

////if (TryGet(Guid.Parse("97574d4a-134b-abb9-a53c-b72beba4bc38"), out Employees? employee))
////{
////    //employee!.Remove();
////    //employee!.Columns.Points = 481;
////    //employee.AsRecord.Manager.AsRecord.Manager.AsRecord
////    //employee!.AsRecord.Age = 85;
////    Console.WriteLine(employee!.AsRecord.EmployeeReferences[2].AsRecord.Age);
////}
////else
////{
////    Console.WriteLine("Failed");
////}

//////Employees.Points.IndexedValueExists(System.Numerics.BigInteger.Parse("452468476496486947785434565478964794641641246475949684312613241645641684742635445194167423454315187685464216416447356471349684764346748"), out Guid hash, out Guid value);
//////Employees.Points.

//Console.WriteLine(count());

//int count(params int[] values)
//{
//    return values.Length;
//}

//Type recordType = typeof(Test);

Test meh = new();

Test meh2 = meh with
{
    Age = 1,
    Score = BigInteger.Parse("23763474835632467358365246245737345"),
    Level = (54, "M")
};

meh.Meh();

//System.Reflection.PropertyInfo[] props = recordType.GetProperties();

//foreach(System.Reflection.PropertyInfo property in props)
//{
//    Console.WriteLine(property.PropertyType);
//}

public record Test : DbRecord
{
    [NotNull]
    public @int Age { get => Table.Get(this); set => Table.Set(this); }
    public @bint Score { get => Table.Get(this); set => Table.Set(this); }
    public @tuple<@int, @string> Level { get => Table.Get(this); set => Table.Set(this); }
}