using BijouDB;
using BijouDB_Test;

if (args.Length == 3 && args[0] == "--child") return StorageTests.RunChild(args[1], args[2]);
Globals.DatabasePath = Path.Combine(Path.GetTempPath(), "bijoudb-regression-" + Guid.NewGuid().ToString("N"));
List<(string Name, Action Run)> cases = new();
void Register(string name, Action run) => cases.Add((name, run));
StreamTests.Register(Register);
SerializationTests.Register(Register);
RelationshipTests.Register(Register);
StorageTests.Register(Register);
CompatibilityTests.Register(Register);
int failures = 0;
foreach ((string Name, Action Run) test in cases)
{
    try
    {
        test.Run();
        Console.WriteLine("PASS " + test.Name);
    }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine("FAIL " + test.Name + Environment.NewLine + error);
    }
}

Console.WriteLine($"{cases.Count - failures}/{cases.Count} regression tests passed.");
return failures == 0 ? 0 : 1;
