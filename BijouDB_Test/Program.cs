using BijouDB_Test.Tables;

Employee employee = new();
employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new() { Type = "Origin" };
computer.Employees += employee;

foreach (Computer comp in employee.Computers.All)
    Console.WriteLine(comp.Type);

computer.Remove();

foreach (Computer comp in employee.Computers.All)
    Console.WriteLine(comp.Type);