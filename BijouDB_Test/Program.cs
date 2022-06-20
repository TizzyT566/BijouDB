using BijouDB;
using BijouDB_Test.Tables;

Computer cpu = new();

for(int i = 0; i < 5; i++)
{
    _ = new Employee
    {
        Computer = cpu,
        Name = i.ToString(),
    };
}

foreach(Employee e in cpu.Employees)
{
    Console.WriteLine(e.Name);
}