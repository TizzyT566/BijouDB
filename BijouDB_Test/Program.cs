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

Child child1 = new()
{
    Name = "David",
    Parent = (person, 1)
};

Child child2 = new()
{
    Name = "Micheal",
    Parent = (person, 2)
};

Child c1 = person.Children[1];
Console.WriteLine(c1.Name);
Console.WriteLine(c1.Parent.Record.Name);
Console.WriteLine(c1.Parent.Record.PhoneNumber);

Child c2 = person.Children[2];
Console.WriteLine(c2.Name);
Console.WriteLine(c2.Parent.Record.Name);
Console.WriteLine(c2.Parent.Record.PhoneNumber);
