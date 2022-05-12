using BijouDB;
using System.Numerics;
using BijouDB.DataTypes;
using BijouDB_Test.Tables;

foreach(string name in Employee.NameColumn.UniqueValues())
{
    Console.WriteLine(name);
}