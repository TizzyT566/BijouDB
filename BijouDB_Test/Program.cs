using BijouDB;
using BijouDB.DataTypes;
using BijouDB_Test.Tables;

//Globals.Logging = true;

//Employee test = new()
//{
//    Name = "Thien Huynh1"
//};

//Computer com1 = new()
//{
//    Employee = test,
//    Type = "Alienware"
//};

//Computer com2 = new()
//{
//    Employee = test,
//    Type = "HP"
//};

//Computer com3 = new()
//{
//    Employee = test,
//    Type = "Origin"
//};

////com1.Remove();

////com2.Remove();

////com3.Remove();

////test.Remove();

//foreach (Computer comp in test.Computers)
//{
//    Console.WriteLine(comp.Type);
//}

////foreach (Employee employee in Record.GetAll<Employee>())
////{
////    Console.WriteLine(employee.Name);
////    Console.WriteLine(Employee.ComputerReferences.HasRecords(employee));
////}

Person person = new()
{
    Name = "TizzyT",
    PhoneNumber = (555, 941, 9464)
};

_ = new Child()
{
    Age = 12,
    Data = new byte[] { 15, 64, 156, 44, 49, 91, 241 },
    Parent = (Record: person, Index: "David")
};

Console.WriteLine(person.Children["David"].Json);

//_ = new Child()
//{
//    Age = 10,
//    Parent = (Record: person, Index: "Lisa")
//};

//Child c1 = person.Children["David"];
//Console.WriteLine(c1.Age);
//Console.WriteLine(c1.Parent.Record.Name);
//Console.WriteLine(c1.Parent.Record.PhoneNumber);

//Child c2 = person.Children["Lisa"];
//Console.WriteLine(c2.Age);
//Console.WriteLine(c2.Parent.Record.Name);
//Console.WriteLine(c2.Parent.Record.PhoneNumber);
