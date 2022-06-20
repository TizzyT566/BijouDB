using BijouDB_Test.Tables;

Computer cpu = new();

for(int i = 0; i < 5; i++)
{
    _ = new Employee
    {
        Computer = cpu
    };
}

foreach(Employee e in cpu.Employees)
{
    Console.WriteLine(e.Json);
}