using BijouDB_Test.Tables;

Employee tz1 = new()
{
    Name = "TizzyT1"
};

Computer computer = new()
{
    Name = "HP"
};

tz1.Computers += computer;

Console.WriteLine(tz1.Json);

Console.WriteLine();

Console.WriteLine(computer.Json);

computer.Remove();

Console.WriteLine(tz1.Json);

Console.WriteLine();

Console.WriteLine(computer.Json);

