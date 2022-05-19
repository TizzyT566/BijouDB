using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BijouDB;

public static class Json
{
    private static readonly Dictionary<Type, Func<object, string>> _formatters = new();

    private static bool _expand;
    private static int _depth;
    private static HashSet<Guid> _references = new();

    private static int _lock;

    static Json()
    {
        _formatters.Add(typeof(BigInteger), o => o.ToString());
        _formatters.Add(typeof(byte[]), o => $"\"data:application/octet-stream;base64,{Convert.ToBase64String((byte[])o)}\"");
        _formatters.Add(typeof(bool), o => (bool)o ? "true" : "false");
        _formatters.Add(typeof(byte), o => o.ToString());
        _formatters.Add(typeof(char), o => $"\"{o}\"");
        _formatters.Add(typeof(decimal), o => $"\"{o}\"");
        _formatters.Add(typeof(float), o => o.ToString());
        _formatters.Add(typeof(int), o => o.ToString());
        _formatters.Add(typeof(long), o => o.ToString());
        _formatters.Add(typeof(sbyte), o => o.ToString());
        _formatters.Add(typeof(short), o => o.ToString());
        _formatters.Add(typeof(string), o => $"\"{o}\"");
        _formatters.Add(typeof(DateTime), o => ((DateTime)o).Ticks.ToString());
        _formatters.Add(typeof(uint), o => o.ToString());
        _formatters.Add(typeof(ulong), o => o.ToString());
        _formatters.Add(typeof(ushort), o => o.ToString());
        _formatters.Add(typeof(Guid), o => $"\"{o}\"");
    }

    /// <summary>
    /// Tries to add a formatter for a spicified type to Json.
    /// </summary>
    /// <param name="type">The type to add a formatter for.</param>
    /// <param name="formatter">The type formatter.</param>
    /// <returns>true if formatter was successfully added, otherwise false.</returns>
    public static bool TryAddFormatter(Type type, Func<object, string> formatter)
    {
        if (type is null) return false;
        if (typeof(Record).Equals(type)) return false;
        return _formatters.TryAdd(type, formatter);
    }

    public static string ToJson(this object @this, bool expand = true, int depth = 0)
    {
        try
        {
            SpinWait.SpinUntil(() => Interlocked.Exchange(ref _lock, 1) == 0);
            _expand = expand;
            _depth = depth;
            string json = ToJson(@this);
            _references.Clear();
            return json;
        }
        catch (Exception)
        {
            return "null";
        }
        finally
        {
            Interlocked.Exchange(ref _lock, 0);
        }
    }

    private static string ToJson(object obj)
    {
        if (obj is null) return "null";

        // if record
        if (obj is Record record) return RecordToJson(record);

        // if junction
        if (IsJunction(obj)) return JunctionToJson(obj);

        if (_formatters.TryGetValue(obj.GetType(), out Func<object, string> formatter))
            return formatter(obj);

        // if array
        if (obj.GetType().IsArray) return ArrayToJson(obj);

        // if tuple
        if (IsTuple(obj)) return TupleToJson(obj);

        return $"\"{obj}\"";
    }

    private static string JunctionToJson(object obj)
    {
        dynamic objDyn = obj;
        object[] relations = objDyn.All;
        return ArrayToJson(relations);
    }

    private static string ArrayToJson(object obj)
    {
        object[] arr = (object[])obj;
        StringBuilder sb = new("[");
        List<string> parts = new();
        foreach (object item in arr) parts.Add(ToJson(item));
        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string RecordToJson(Record record)
    {
        StringBuilder sb = new("{");
        Type t = record.GetType();

        sb.Append($"\"{nameof(record.Id)}\":\"{record.Id}\",\"{t.FullName}\":{{");

        List<string> parts = new();

        if (_expand && _depth >= 0 && !_references.Contains(record.Id))
        {
            Interlocked.Decrement(ref _depth);
            _references.Add(record.Id);
            PropertyInfo[] properties = t.GetProperties();
            foreach (PropertyInfo property in properties)
                if (JsonAttribute.HasAttribute(property))
                {
                    if (IsTuple(property) && TupleObjectAttribute.HasAttribute(property, out string[] labels))
                        parts.Add($"\"{property.Name}\":{TupleObjectToJson(property.GetValue(record), labels)}");
                    else
                        parts.Add($"\"{property.Name}\":{ToJson(property.GetValue(record))}");
                }
            _references.Remove(record.Id);
            Interlocked.Increment(ref _depth);
        }

        return sb.Append($"{string.Join(",", parts)}}}}}").ToString();
    }

    private static string TupleToJson(object obj)
    {
        StringBuilder sb = new("[");

        List<string> parts = new();

        foreach (object item in ValueTupleValues(obj))
            parts.Add(ToJson(item));

        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string TupleObjectToJson(object obj, string[] labels)
    {
        StringBuilder sb = new("{");

        List<string> parts = new();

        int i = 0;
        foreach (object item in ValueTupleValues(obj))
        {
            string name = i < labels.Length ? labels[i++] : i++.ToString();
            parts.Add($"{ToJson(name)}:{ToJson(item)}");
        }

        return sb.Append($"{string.Join(",", parts)}}}").ToString();
    }

    private static IEnumerable ValueTupleValues(object tuple) =>
        tuple.GetType().GetFields().Select(feild => feild.GetValue(tuple));

    private static bool IsTuple(object obj)
    {
        Type type;

        if (obj is PropertyInfo prop) type = prop.PropertyType;
        else if (obj is FieldInfo field) type = field.FieldType;
        else type = obj.GetType();

        if (!type.IsGenericType) return false;
        Type? def = type.GetGenericTypeDefinition();
        for (int i = 2; ; ++i)
        {
            Type? tupleType = Type.GetType("System.ValueTuple`" + i);
            if (tupleType is null) return false;
            if (def == tupleType) return true;
        }
    }

    private static bool IsJunction(object obj)
    {
        Type type = obj.GetType();
        if (!type.IsGenericType) return false;
        Type genType = type.GetGenericTypeDefinition();
        return genType.FullName == "BijouDB.Relational`2+Junc";
    }

    private static bool IsRecord(object obj)
    {
        if (obj is PropertyInfo prop) return prop.PropertyType.IsSubclassOf(typeof(Record));
        if (obj is FieldInfo field) return field.FieldType.IsSubclassOf(typeof(Record));
        return obj is Record;
    }
}
