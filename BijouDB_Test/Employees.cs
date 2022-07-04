using BijouDB;

namespace BijouDB_Test.Tables;

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Relational<Employee, Employee> FriendRelational;
    public static readonly Relational<Employee, Employee> EnemyRelational;

    [Json]
    public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json]
    public Relational<Employee, Employee>.Junc Friends
    { get => FriendRelational.To(this); set { } }

    [Json]
    public Relational<Employee, Employee>.Junc Enemies
    { get => EnemyRelational.To(this); set { } }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn, BeforeGet: () => Console.WriteLine("Got Something"))
        .Add(out FriendRelational, FriendRelational!)
        .Add(out EnemyRelational, FriendRelational!);

    public override void BeforeAdd()
    {
        Console.WriteLine($"Added {Id}");
    }

    public override void AfterRemove()
    {
        Console.WriteLine($"Removed {Id}");
    }
}