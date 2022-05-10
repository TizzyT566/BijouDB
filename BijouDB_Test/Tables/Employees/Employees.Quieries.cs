using BijouDB;
using BijouDB.DataTypes;

namespace BijouDB_Test.Tables;

public partial class Employees : Table
{

}

public record Employee(tuple<@string, @int> tup) : Record;