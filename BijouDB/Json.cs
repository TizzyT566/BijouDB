using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BijouDB;

public static class Json
{
    private static readonly Dictionary<Type, Func<object, string>> _formatters = new();

    static Json()
    {
        _formatters.Add(typeof(BigInteger), o => o.ToString());
        _formatters.Add(typeof(byte[]), o => Q($"data:application/octet-stream;base64, {Convert.ToBase64String((byte[])o)}"));
        _formatters.Add(typeof(bool), o => (bool)o ? "true" : "false");
        _formatters.Add(typeof(byte), o => o.ToString());
        _formatters.Add(typeof(char), o => Q(o));
        _formatters.Add(typeof(decimal), o => Q(o));
        _formatters.Add(typeof(float), o => o.ToString());
        _formatters.Add(typeof(int), o => o.ToString());
        _formatters.Add(typeof(long), o => o.ToString());
        _formatters.Add(typeof(sbyte), o => o.ToString());
        _formatters.Add(typeof(short), o => o.ToString());
        _formatters.Add(typeof(string), o => Q(o));
        _formatters.Add(typeof(DateTime), o => ((DateTime)o).Ticks.ToString());
        _formatters.Add(typeof(uint), o => o.ToString());
        _formatters.Add(typeof(ulong), o => o.ToString());
        _formatters.Add(typeof(ushort), o => o.ToString());
        _formatters.Add(typeof(Guid), o => Q(o));
    }

    /// <summary>
    /// Tries to add a formatter for a spicified type to Json.
    /// </summary>
    /// <param name="type">The type to add a formatter for.</param>
    /// <param name="formater">The type formatter.</param>
    /// <returns></returns>
    public static bool TryAddFormatter(Type type, Func<object, string> formater)
    {
        if (type is null) return false;
        if (typeof(Record).Equals(type)) return false;
        if (_formatters.ContainsKey(type)) return false;
        _formatters.Add(type, formater);
        return true;
    }

    private static string Q(object obj) => $"\"{obj}\"";

    internal static string ToJson(object obj, HashSet<Guid> references = null!)
    {
        if (obj is null) return "null";

        if (_formatters.TryGetValue(obj.GetType(), out Func<object, string> formatter))
            return formatter(obj);

        references ??= new();

        // if junction
        if (IsJunction(obj)) return JunctionToJson(obj, references);

        // if record
        if (obj is Record record) return RecordToJson(record, references);

        // if tuple
        if (IsTuple(obj)) return TupleToJson(obj, references);

        // if array
        if (obj.GetType().IsArray) return ArrayToJson(obj, references);

        return Q(obj);
    }

    private static string JunctionToJson(object obj, HashSet<Guid> references)
    {
        dynamic objDyn = obj;
        object[] relations = objDyn.All();
        return ArrayToJson(relations, references);
    }

    private static string ArrayToJson(object obj, HashSet<Guid> references)
    {
        object[] arr = (object[])obj;

        StringBuilder sb = new("[");

        List<string> parts = new();

        foreach (object item in arr) parts.Add(ToJson(item, references));

        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string RecordToJson(Record record, HashSet<Guid> references)
    {
        StringBuilder sb = new("{");
        Type t = record.GetType();

        sb.Append($"{ToJson(nameof(record.Id), references)}:{ToJson(record.Id, references)},{ToJson(t.FullName, references)}:{{");

        List<string> parts = new();

        if (!references.Contains(record.Id))
        {
            references.Add(record.Id);
            PropertyInfo[] properties = t.GetProperties();
            foreach (PropertyInfo property in properties)
                if (JsonAttribute.HasAttribute(property))
                {
                    if (IsRecord(property))
                        parts.Add($"{ToJson(property.Name, references)}:{ToJson(property.GetValue(record), references)}");
                    else if (IsTuple(property) && TupleObjectAttribute.HasAttribute(property, out string[] labels))
                        parts.Add($"{ToJson(property.Name, references)}:{TupleObjectToJson(property.GetValue(record), labels, references)}");
                    else
                        parts.Add($"{ToJson(property.Name, references)}:{ToJson(property.GetValue(record), references)}");
                }
            references.Remove(record.Id);
        }

        return sb.Append($"{string.Join(",", parts)}}}}}").ToString();
    }

    public static string TupleToJson(object obj, HashSet<Guid> references)
    {
        StringBuilder sb = new("[");

        List<string> parts = new();

        foreach (object item in ValueTupleValues(obj))
            parts.Add(ToJson(item, references));

        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    public static string TupleObjectToJson(object obj, string[] labels, HashSet<Guid> references)
    {
        StringBuilder sb = new("{");

        List<string> parts = new();

        int i = 0;
        foreach (object item in ValueTupleValues(obj))
        {
            string name = i < labels.Length ? labels[i++] : i++.ToString();
            parts.Add($"{ToJson(name, references)}:{ToJson(item, references)}");
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
