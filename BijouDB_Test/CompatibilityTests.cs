using BijouDB;

namespace BijouDB_Test;

internal static class CompatibilityTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("legacy named parameters and relationship overloads remain available", () =>
        {
            CompatibilityLeft left = new();
            CompatibilityRight right = new();
            CompatibilityLeft.Links.To(left).Add(right);
            TestAssert.Equal(left.Id, CompatibilityRight.Links.To(right).All.Single().Id);
            UniqueTestRow row = new()
            {
                Value = 45678
            };
            TestAssert.True(UniqueTestRow.ValueColumn.Contains<UniqueTestRow>(data: 45678));
        });
        test("single-input intersections preserve handles and duplicates", () =>
        {
            UniqueTestRow row = new();
            UniqueTestRow[] input = new[] { row, row };
            IEnumerable<UniqueTestRow> result = Record.WithValues(input);
            TestAssert.True(ReferenceEquals(input, result));
            TestAssert.Equal(2, result.Count());
            TestAssert.True(ReferenceEquals(row, result.First()));
        });
        test("record hashes remain stable after first property access", () =>
        {
            UniqueTestRow row = new();
            HashSet<Record> set = new()
            { row };
            int before = row.GetHashCode();
            row.Value = 34567;
            TestAssert.Equal(before, row.GetHashCode());
            TestAssert.True(set.Contains(row));
            Dictionary<Record, int> logical = new(row)
            {
                [row] = 1
            };
            TestAssert.True(Record.TryGet<UniqueTestRow>(row.Id, out UniqueTestRow? restored));
            TestAssert.Equal(1, logical[restored!]);
        });
    }
}

public sealed class CompatibilityLeft : Record
{
    public static readonly Column<@int> ValueColumn;
    public static readonly Relational<CompatibilityLeft, CompatibilityRight> Links;

    static CompatibilityLeft() => _ = ~SchemaBuilder<CompatibilityLeft>.Add(out ValueColumn).Add(out Links, _: null!);
}

public sealed class CompatibilityRight : Record
{
    public static readonly Relational<CompatibilityRight, CompatibilityLeft> Links;

    static CompatibilityRight() => _ = ~SchemaBuilder<CompatibilityRight>.Add(out Links, _: CompatibilityLeft.Links);
}
