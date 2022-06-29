using BijouDB_Test.Tables;

Employee tz1 = new()
{
    Name = "TizzyT1"
};
Employee tz2 = new()
{
    Name = "TizzyT1"
};
Employee tz3 = new()
{
    Name = "TizzyT1"
};
tz1.Friends += tz2;
tz1.Enemies += tz3;

Console.WriteLine(tz1.Json);