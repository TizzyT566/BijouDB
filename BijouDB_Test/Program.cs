using BijouDB_Test.Tables;

int iterations = 100000;

Employee tz1 = new()
{
    Name = "TizzyT1"
};
Employee tz2 = new()
{
    Name = "TizzyT2"
};
Employee tz3 = new()
{
    Name = "TizzyT3"
};

long start = System.Diagnostics.Stopwatch.GetTimestamp();

string name1 = null!, name2 = null!, name3 = null!;

for (int i = 0; i < iterations; i++)
{
    name1 = tz1.Name;
    name2 = tz2.Name;
    name3 = tz3.Name;
}

long stop = System.Diagnostics.Stopwatch.GetTimestamp();

Console.WriteLine(TimeSpan.FromTicks(stop - start).TotalMilliseconds);
Console.WriteLine(name1);
Console.WriteLine(name2);
Console.WriteLine(name3);

