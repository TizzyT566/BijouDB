using BijouDB;
using BijouDB_Test.Tables;

foreach(string type in Record.Types)
{
    Console.WriteLine(type);
}

//int iterations = 100000;

//Employee tz1 = new("TizzyT1"); ;
//Employee tz2 = new("");
//Employee tz3 = new("TizzyT3");

//long start = System.Diagnostics.Stopwatch.GetTimestamp();

//string name1 = null!, name2 = null!, name3 = null!;

//for (int i = 0; i < iterations; i++)
//{
//    name1 = tz1.Name;
//    name2 = tz2.Name;
//    name3 = tz3.Name;
//}

//long stop = System.Diagnostics.Stopwatch.GetTimestamp();

//Console.WriteLine(TimeSpan.FromTicks(stop - start).TotalMilliseconds);

//foreach (Employee e in Record.GetAll<Employee>())
//{
//    e.TryRemove(out _);
//    try
//    {
//        e.Name = "Test";
//        Console.WriteLine(e.Json);
//    }
//    catch (Exception)
//    { }
//}