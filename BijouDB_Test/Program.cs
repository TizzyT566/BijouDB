using BijouDB;
using System.Numerics;
using BijouDB.DataTypes;
using BijouDB_Test.Tables;

Employee test = new()
{
    Name = "Thien Huynh"
};

Employee test2 = new()
{
    Name = "e1",
    Manager = test
};

Employee test3 = new()
{
    Name = "e2",
    Manager = test
};

Employee test4 = new()
{
    Name = "e3",
    Manager = test
};

Employee test5 = new()
{
    Name = "e4",
    Manager = test
};

//foreach (Employee e in test.Employees)
//{
//    Console.WriteLine(e.Name);
//}

////if (Record.TryGet(Guid.Parse("000005e1-0000-0000-0000-000000000000"), out Employee? employee))
////{
////    Console.WriteLine(employee!.Manager!.Name);
////}

foreach (Employee? employee in Employee.ManagerColumn.UniqueValues())
{
    Console.WriteLine(employee?.Name);
}