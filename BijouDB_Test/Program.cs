using BijouDB_Test.Tables;

//Computer cpu = new();

//for(int i = 0; i < 5; i++)
//{
//    _ = new Employee
//    {
//        Computer = cpu,
//        BigInt = System.Numerics.BigInteger.Parse("416274654132468546418749643154954374412811432848549456432413484756413164968475345468158473574354544549")
//    };
//}

foreach(Employee e in BijouDB.Record.GetAll<Employee>())
{
    Console.WriteLine(e.Json);
}