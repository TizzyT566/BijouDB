using BijouDB_Test.Tables;

int iterations = 100000;

Employee tz = new()
{
    Name = "TizzyT"
};

long start = System.Diagnostics.Stopwatch.GetTimestamp();

string name = null!;

for (int i = 0; i < iterations; i++)
{
    name = tz.Name;
}

long stop = System.Diagnostics.Stopwatch.GetTimestamp();

Console.WriteLine(TimeSpan.FromTicks(stop - start).TotalMilliseconds);
Console.WriteLine(name);

