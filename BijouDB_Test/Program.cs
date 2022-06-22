using BijouDB_Test.Tables;
using BijouDB;

Computer cpu = new();

//Employee employee = new();
//employee.Computers += cpu;-

foreach (Computer e in Record.GetAll<Computer>())
{
    Console.WriteLine(e.ToJson(0));
}