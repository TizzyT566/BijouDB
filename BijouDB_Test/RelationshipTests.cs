using BijouDB;

namespace BijouDB_Test;

internal static class RelationshipTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("relationships share storage across different schema positions and persist fresh targets", () =>
        {
            RelationshipLeft left = new();
            RelationshipRight right = new();
            left.Rights.Add(right);
            TestAssert.Equal(right.Id, left.Rights.All.Single().Id);
            TestAssert.Equal(left.Id, right.Lefts.All.Single().Id);
            TestAssert.True(Record.TryGet<RelationshipRight>(right.Id, out _));
            // Returned matches are a snapshot rather than an iterator holding the storage lock.
            IEnumerable<RelationshipRight> snapshot = left.Rights.All;
            left.Rights.Clear();
            TestAssert.Equal(1, snapshot.Count());
            TestAssert.Equal(0, right.Lefts.All.Count());
        });
        test("named relationships between the same types stay independent", () =>
        {
            RelationshipNamedLeft left = new();
            RelationshipNamedRight right = new();
            left.Owns.Add(right);
            TestAssert.Equal(1, right.Owners.All.Count());
            TestAssert.Equal(0, right.Users.All.Count());
            TestAssert.Equal(0, left.Uses.All.Count());
            right.Users.Add(left);
            TestAssert.Equal(1, left.Uses.All.Count());
            left.Owns.Remove(right);
            TestAssert.Equal(0, right.Owners.All.Count());
            TestAssert.Equal(1, right.Users.All.Count());
            TestAssert.True(left.TryRemove(out _));
            TestAssert.Equal(0, right.Users.All.Count());
        });
        test("legacy relationship directories are read and removed in both directions", () =>
        {
            RelationshipLeft left = new();
            RelationshipRight right = new();
            string first = typeof(RelationshipLeft).FullName!, second = typeof(RelationshipRight).FullName!;
            string pair = first.CompareTo(second) < 0 ? first + "-" + second : second + "-" + first;
            string directory = Path.Combine(Globals.DatabasePath, pair + "_0");
            Directory.CreateDirectory(directory);
            string a = left.Id.ToString(), b = right.Id.ToString();
            string filename = StringComparer.Ordinal.Compare(a, b) <= 0 ? a + "." + b : b + "." + a;
            File.WriteAllBytes(Path.Combine(directory, filename), Array.Empty<byte>());
            File.WriteAllText(Path.Combine(directory, "malformed." + a), "ignored");
            TestAssert.Equal(right.Id, left.Rights.All.Single().Id);
            TestAssert.Equal(left.Id, right.Lefts.All.Single().Id);
            left.Rights.Remove(right);
            TestAssert.Equal(0, left.Rights.All.Count());
            TestAssert.Equal(0, right.Lefts.All.Count());
        });
        test("removing an unused relationship target does not persist it", () =>
        {
            RelationshipLeft left = new();
            RelationshipRight unused = new();
            left.Rights.Remove(unused);
            TestAssert.True(unused._id is null);
            TestAssert.Equal(0, left.Rights.All.Count());
        });
        test("restricted deletion reports failure and preserves all parent values", () =>
        {
            RelationshipParent parent = new()
            {
                Value = 17
            };
            RelationshipChild child = new()
            {
                Parent = parent
            };
            TestAssert.True(!parent.TryRemove(out Exception? error));
            TestAssert.True(error is not null);
            TestAssert.True(Record.TryGet<RelationshipParent>(parent.Id, out _));
            TestAssert.Equal(17, parent.Value);
            TestAssert.Equal(child.Id, RelationshipParent.Children.To(parent).Single().Id);
            TestAssert.True(child.TryRemove(out _));
            TestAssert.True(parent.TryRemove(out _));
            TestAssert.True(!Record.TryGet<RelationshipParent>(parent.Id, out _));
        });
        test("relationship write errors propagate", () =>
        {
            RelationshipLeft left = new();
            RelationshipRight right = new();
            string path = Path.Combine(Globals.DatabasePath, "relationship-directory-is-a-file");
            File.WriteAllText(path, "fixture");
            Relational<RelationshipLeft, RelationshipRight>.Junc junction = new(left, path);
            TestAssert.Throws<IOException>(() => junction.Add(right));
        });
        test("deleted endpoints cannot be linked again through an existing junction", () =>
        {
            RelationshipLeft left = new();
            RelationshipRight right = new();
            Relational<RelationshipLeft, RelationshipRight>.Junc junction = left.Rights;
            junction.Add(right);
            TestAssert.True(right.TryRemove(out _));
            TestAssert.Throws<FileNotFoundException>(() => junction.Add(right));
            RelationshipRight other = new();
            TestAssert.True(left.TryRemove(out _));
            TestAssert.Throws<FileNotFoundException>(() => junction.Add(other));
            TestAssert.True(other._id is null);
        });
    }
}

public sealed class RelationshipLeft : Record
{
    public static readonly Column<@int> ValueColumn;
    public static readonly Relational<RelationshipLeft, RelationshipRight> RightRelations;

    static RelationshipLeft() => _ = ~SchemaBuilder<RelationshipLeft>.Add(out ValueColumn).Add(out RightRelations, RelationshipRight.LeftRelations);

    public Relational<RelationshipLeft, RelationshipRight>.Junc Rights => RightRelations.To(this);
}

public sealed class RelationshipRight : Record
{
    public static readonly Relational<RelationshipRight, RelationshipLeft> LeftRelations;

    static RelationshipRight() => _ = ~SchemaBuilder<RelationshipRight>.Add(out LeftRelations, RelationshipLeft.RightRelations);

    public Relational<RelationshipRight, RelationshipLeft>.Junc Lefts => LeftRelations.To(this);
}

public sealed class RelationshipNamedLeft : Record
{
    public static readonly Relational<RelationshipNamedLeft, RelationshipNamedRight> OwnRelations;
    public static readonly Relational<RelationshipNamedLeft, RelationshipNamedRight> UseRelations;

    static RelationshipNamedLeft() => _ = ~SchemaBuilder<RelationshipNamedLeft>.Add(out OwnRelations, "owns").Add(out UseRelations, "uses");

    public Relational<RelationshipNamedLeft, RelationshipNamedRight>.Junc Owns => OwnRelations.To(this);

    public Relational<RelationshipNamedLeft, RelationshipNamedRight>.Junc Uses => UseRelations.To(this);
}

public sealed class RelationshipNamedRight : Record
{
    public static readonly Relational<RelationshipNamedRight, RelationshipNamedLeft> OwnerRelations;
    public static readonly Relational<RelationshipNamedRight, RelationshipNamedLeft> UserRelations;

    static RelationshipNamedRight() => _ = ~SchemaBuilder<RelationshipNamedRight>.Add(out OwnerRelations, "owns").Add(out UserRelations, "uses");

    public Relational<RelationshipNamedRight, RelationshipNamedLeft>.Junc Owners => OwnerRelations.To(this);

    public Relational<RelationshipNamedRight, RelationshipNamedLeft>.Junc Users => UserRelations.To(this);
}

public sealed class RelationshipParent : Record
{
    public static readonly Column<@int> ValueColumn;
    public static readonly Reference<@record<RelationshipParent>, RelationshipChild> Children;

    static RelationshipParent() => _ = ~SchemaBuilder<RelationshipParent>.Add(out ValueColumn).Add(out Children, () => RelationshipChild.ParentColumn);

    public int Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }
}

public sealed class RelationshipChild : Record
{
    public static readonly Column<@record<RelationshipParent>> ParentColumn;

    static RelationshipChild() => _ = ~SchemaBuilder<RelationshipChild>.Add(out ParentColumn);

    public RelationshipParent Parent { get => ParentColumn.Get(this); set => ParentColumn.Set(this, value); }
}
