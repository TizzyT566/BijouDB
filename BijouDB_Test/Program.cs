using BijouDB;
using BijouDB_Test.Tables;

//Employee employee = new();
//employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new();
//computer.Employees += employee;

computer.Save();

//Console.WriteLine(computer.ToJson(true, 3));

//Console.WriteLine(Record.Types.ToJson());

Console.WriteLine(Record.Get(computer.GetType().FullName!, computer.Id.ToString(), "Type"));