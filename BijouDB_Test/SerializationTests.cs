using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using BijouDB;

namespace BijouDB_Test;

internal static class SerializationTests
{
    public static void Register(Action<string, Action> test)
    {
        test("blob cache preserves data and array ownership", BlobOwnership);
        test("tuple arities 2 through 8 round-trip struct and class components", TupleRoundTrips);
        test("DateTime preserves ticks and Kind including legacy values", DateTimeRoundTrips);
        test("JSON supports value-type arrays and enumerables", JsonCollections);
        test("JSON escapes controls, characters and fallback text", JsonEscaping);
        test("JSON numbers are invariant and BigInteger has no JS suffix", JsonNumbers);
        test("JSON rejects nonfinite numbers and propagates formatter failures", JsonFailures);
        test("JSON formatters can call ToJson recursively", ReentrantFormatter);
        test("JSON serialization has independent concurrent traversal state", ConcurrentJson);
        test("JSON materialization does not invoke user constructors", JsonMaterialization);
        test("JSON flattens long tuples and handles collection cycles", JsonTuplesAndCycles);
        test("JSON respects record depth and property levels", JsonDepthAndLevel);
    }

    private static void BlobOwnership()
    {
        byte[] input = { 1, 2, 3, 4, 5 };
        SerializationBlobRecord row = new()
        {
            Value = input,
            NullableValue = input
        };
        input[0] = 90;
        TestAssert.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }, row.Value);
        byte[] result = row.Value;
        result[1] = 80;
        TestAssert.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }, row.Value);
        byte[] nullableResult = row.NullableValue!;
        nullableResult[2] = 70;
        TestAssert.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }, row.NullableValue!);
        TestAssert.True(Record.TryGet(row.Id, out SerializationBlobRecord? restored));
        TestAssert.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }, restored!.Value);
        @blob empty = default;
        TestAssert.Equal(0, ((byte[])empty).Length);
        @blob.nullable missing = (byte[]?)null;
        TestAssert.True((byte[]?)missing is null);
    }

    private static void TupleRoundTrips()
    {
        Assembly assembly = typeof(IDataType).Assembly;
        for (int arity = 2; arity <= 8; arity++)
        {
            foreach (Type component in new[] { typeof(@int), typeof(@int.nullable) })
            {
                Type[] arguments = Enumerable.Repeat(component, arity).ToArray();
                Type wrapper = assembly.GetType("BijouDB.tuple`" + arity)!.MakeGenericType(arguments);
                CheckTuple(wrapper, arity);
                Type nullableWrapper = wrapper.GetNestedType("nullable")!.MakeGenericType(arguments);
                CheckTuple(nullableWrapper, arity);
                IDataType empty = (IDataType)Activator.CreateInstance(nullableWrapper)!;
                IDataType restoredEmpty = RoundTrip(empty, nullableWrapper);
                TestAssert.True(nullableWrapper.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(restoredEmpty)is null);
            }
        }

        tuple<@int.nullable, @int> defaultWithClass = new();
        using MemoryStream stream = new();
        defaultWithClass.Serialize(stream);
        stream.Position = 0;
        tuple<@int.nullable, @int> restored = new();
        restored.Deserialize(stream);
        (@int.nullable, @int) values = ((@int.nullable, @int))restored;
        TestAssert.True((int?)values.Item1 is null);
        TestAssert.Equal(0, (int)values.Item2);
    }

    private static void CheckTuple(Type wrapper, int arity)
    {
        FieldInfo field = wrapper.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Type tupleType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        object tuple = MakeTuple(tupleType, 1);
        MethodInfo conversion = wrapper.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "op_Implicit" && method.ReturnType == wrapper);
        IDataType value = (IDataType)conversion.Invoke(null, new[] { tuple })!;
        object restored = RoundTrip(value, wrapper);
        object restoredTuple = field.GetValue(restored)!;
        int[] numbers = FlattenTuple(restoredTuple)
            .Select(item => item is @int number ? (int)number : ((int?)(@int.nullable)item!).GetValueOrDefault())
            .ToArray();
        TestAssert.SequenceEqual(Enumerable.Range(1, arity), numbers);
    }

    private static IDataType RoundTrip(IDataType value, Type type)
    {
        using MemoryStream stream = new();
        value.Serialize(stream);
        stream.Position = 0;
        IDataType result = (IDataType)Activator.CreateInstance(type)!;
        result.Deserialize(stream);
        TestAssert.Equal(stream.Length, stream.Position);
        return result;
    }

    private static object MakeTuple(Type type, int start)
    {
        Type[] arguments = type.GetGenericArguments();
        object[] values = new object[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
            values[i] = arguments[i].Name.StartsWith("ValueTuple`", StringComparison.Ordinal) ? MakeTuple(arguments[i], start + i) : arguments[i] == typeof(@int) ? (@int)(start + i) : (@int.nullable)(int?)(start + i);

        return Activator.CreateInstance(type, values)!;
    }

    private static IEnumerable<object?> FlattenTuple(object tuple)
    {
        foreach (FieldInfo field in tuple.GetType().GetFields())
            if (field.Name == "Rest")
                foreach (object? nested in FlattenTuple(field.GetValue(tuple)!))
                    yield return nested;
            else
                yield return field.GetValue(tuple);
    }

    private static void DateTimeRoundTrips()
    {
        foreach (DateTimeKind kind in Enum.GetValues<DateTimeKind>())
            foreach (long ticks in new[] { 0L, new DateTime(2026, 11, 11, 13, 14, 15).Ticks, DateTime.MaxValue.Ticks })
            {
                DateTime original = new(ticks, kind);
                time restored = (@time)RoundTrip((@time)original, typeof(@time));
                TestAssert.Equal(original.Ticks, ((DateTime)restored).Ticks);
                TestAssert.Equal(kind, ((DateTime)restored).Kind);
                time.nullable nullable = (@time.nullable)RoundTrip((@time.nullable)(DateTime?)original, typeof(@time.nullable));
                TestAssert.Equal(kind, ((DateTime?)nullable)!.Value.Kind);
                TestAssert.Equal(ticks, ((DateTime?)nullable)!.Value.Ticks);
            }

        using MemoryStream legacy = new(BitConverter.GetBytes(new DateTime(2001, 2, 3).Ticks));
        time old = new();
        old.Deserialize(legacy);
        TestAssert.Equal(DateTimeKind.Unspecified, ((DateTime)old).Kind);
        TestAssert.Equal(new DateTime(2001, 2, 3), (DateTime)old);
        @time.nullable missing = (DateTime?)null;
        TestAssert.True((DateTime?)(@time.nullable)RoundTrip(missing, typeof(@time.nullable))is null);
    }

    private static void JsonCollections()
    {
        TestAssert.Equal("[1,2]", BijouDB.Json.ToJson(new[] { 1, 2 }));
        TestAssert.Equal("[1,2]", BijouDB.Json.ToJson(new List<int> { 1, 2 }));
        TestAssert.Equal("[1,2]", BijouDB.Json.ToJson(Enumerable.Range(1, 2)));
        TestAssert.Equal("[1,null,2]", BijouDB.Json.ToJson(new int? [] { 1, null, 2 }));
        TestAssert.Equal("[true,false]", BijouDB.Json.ToJson(new[] { true, false }));
        TestAssert.Equal("[]", BijouDB.Json.ToJson(Array.Empty<int>()));
    }

    private static void JsonEscaping()
    {
        string controls = new(Enumerable.Range(0, 32).Select(n => (char)n).ToArray());
        string text = controls + "\"\\\n💎";
        using JsonDocument parsed = JsonDocument.Parse(BijouDB.Json.ToJson(text));
        TestAssert.Equal(text, parsed.RootElement.GetString());
        foreach (char c in new[] { '"', '\\', '\0', '\n', '\u001f' })
        {
            using JsonDocument character = JsonDocument.Parse(BijouDB.Json.ToJson(c));
            TestAssert.Equal(c.ToString(), character.RootElement.GetString());
        }

        using JsonDocument fallback = JsonDocument.Parse(BijouDB.Json.ToJson(new SerializationFallbackText()));
        TestAssert.Equal("\"\u0001\\", fallback.RootElement.GetString());
    }

    private static void JsonNumbers()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            TestAssert.Equal("1.5", BijouDB.Json.ToJson(1.5));
            TestAssert.Equal("1.5", BijouDB.Json.ToJson(1.5f));
            TestAssert.Equal("\"1.5\"", BijouDB.Json.ToJson(1.5m));
            BigInteger huge = BigInteger.Pow(10, 50) + 42;
            using JsonDocument number = JsonDocument.Parse(BijouDB.Json.ToJson(huge));
            TestAssert.Equal(huge.ToString(CultureInfo.InvariantCulture), number.RootElement.GetRawText());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static void JsonFailures()
    {
        TestAssert.Throws<InvalidOperationException>(() => BijouDB.Json.ToJson(double.NaN));
        TestAssert.Throws<InvalidOperationException>(() => BijouDB.Json.ToJson(double.PositiveInfinity));
        TestAssert.Throws<InvalidOperationException>(() => BijouDB.Json.ToJson(float.NegativeInfinity));
        TestAssert.True(BijouDB.Json.TryAddFormatter(typeof(SerializationFailingFormat), _ => throw new InvalidOperationException("formatter failure")));
        TestAssert.Throws<InvalidOperationException>(() => BijouDB.Json.ToJson(new SerializationFailingFormat()));
    }

    private static void ReentrantFormatter()
    {
        TestAssert.True(BijouDB.Json.TryAddFormatter(typeof(SerializationReentrantFormat), value => BijouDB.Json.ToJson(((SerializationReentrantFormat)value).Value)));
        TestAssert.Equal("[1,2]", BijouDB.Json.ToJson(new SerializationReentrantFormat { Value = new[] { 1, 2 }}));
    }

    private static void ConcurrentJson()
    {
        SerializationConstructorRecord row = new(41);
        Parallel.For(0, 100, i =>
        {
            using JsonDocument parsed = JsonDocument.Parse(BijouDB.Json.ToJson(row, i % 2));
            JsonElement body = parsed.RootElement.GetProperty(typeof(SerializationConstructorRecord).FullName!);
            TestAssert.Equal(i % 2 == 1, body.TryGetProperty(nameof(SerializationConstructorRecord.Value), out _));
        });
    }

    private static void JsonMaterialization()
    {
        SerializationConstructorRecord row = new(41);
        int calls = SerializationConstructorRecord.ConstructorCalls;
        using JsonDocument record = JsonDocument.Parse(BijouDB.Json.GetRecord(typeof(SerializationConstructorRecord).FullName!, row.Id.ToString(), 1)!);
        TestAssert.Equal(41, record.RootElement.GetProperty(typeof(SerializationConstructorRecord).FullName!).GetProperty("Value").GetInt32());
        TestAssert.Equal("41", BijouDB.Json.GetProperty(typeof(SerializationConstructorRecord).FullName!, row.Id.ToString(), "Value"));
        TestAssert.Equal(calls, SerializationConstructorRecord.ConstructorCalls);
        TestAssert.True(BijouDB.Json.GetRecord(typeof(SerializationConstructorRecord).FullName!, Guid.NewGuid().ToString(), 1)is null);
        TestAssert.True(BijouDB.Json.GetRecord(typeof(SerializationConstructorRecord).FullName!, "not-a-guid")is null);
        TestAssert.True(BijouDB.Json.GetRecord(null!, row.Id.ToString())is null);
        TestAssert.Equal(calls, SerializationConstructorRecord.ConstructorCalls);
    }

    private static void JsonTuplesAndCycles()
    {
        TestAssert.Equal("[1,2,3,4,5,6,7,8,9]", BijouDB.Json.ToJson((1, 2, 3, 4, 5, 6, 7, 8, 9)));
        TestAssert.Equal("[1,2]", BijouDB.Json.ToJson(Tuple.Create(1, 2)));
        ArrayList cycle = new();
        cycle.Add(cycle);
        TestAssert.Throws<InvalidOperationException>(() => BijouDB.Json.ToJson(cycle));
    }

    private static void JsonDepthAndLevel()
    {
        SerializationConstructorRecord row = new(41);
        using JsonDocument shallow = JsonDocument.Parse(BijouDB.Json.ToJson(row, 0));
        TestAssert.Equal(0, shallow.RootElement.GetProperty(typeof(SerializationConstructorRecord).FullName!).EnumerateObject().Count());
        using JsonDocument normal = JsonDocument.Parse(BijouDB.Json.ToJson(row, 1));
        TestAssert.True(!normal.RootElement.GetProperty(typeof(SerializationConstructorRecord).FullName!).TryGetProperty("Secret", out _));
        using JsonDocument elevated = JsonDocument.Parse(BijouDB.Json.ToJson(row, 1, 2));
        TestAssert.Equal(99, elevated.RootElement.GetProperty(typeof(SerializationConstructorRecord).FullName!).GetProperty("Secret").GetInt32());
        TestAssert.True(BijouDB.Json.GetProperty(typeof(SerializationConstructorRecord).FullName!, row.Id.ToString(), "Secret")is null);
    }
}

public sealed class SerializationBlobRecord : Record
{
    public static readonly Column<@blob> ValueColumn;
    public static readonly Column<@blob.nullable> NullableColumn;

    static SerializationBlobRecord() => _ = ~SchemaBuilder<SerializationBlobRecord>.Add(out ValueColumn).Add(out NullableColumn);

    public byte[] Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }

    public byte[]? NullableValue { get => NullableColumn.Get(this); set => NullableColumn.Set(this, value); }
}

public sealed class SerializationConstructorRecord : Record
{
    public static readonly Column<@int> ValueColumn;

    public static int ConstructorCalls;

    static SerializationConstructorRecord() => _ = ~SchemaBuilder<SerializationConstructorRecord>.Add(out ValueColumn);

    public SerializationConstructorRecord()
    {
        ConstructorCalls++;
        throw new NotSupportedException("Retrieval must not invoke this constructor.");
    }

    public SerializationConstructorRecord(int value)
    {
        ConstructorCalls++;
        Value = value;
    }

    [BijouDB.Json]
    public int Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }

    [BijouDB.Json(2)]
    public int Secret => 99;
}

public sealed class SerializationFallbackText
{
    public override string ToString() => "\"\u0001\\";
}

public sealed class SerializationFailingFormat
{
}

public sealed class SerializationReentrantFormat
{
    public object? Value { get; set; }
}
