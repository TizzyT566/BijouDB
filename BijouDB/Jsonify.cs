using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BijouDB;

public static class Jsonify
{
    private static readonly Dictionary<Type, Func<object, string>> _formatters = new();

    static Jsonify()
    {
        _formatters.Add(typeof(BigInteger), o => $"{o}n");
        _formatters.Add(typeof(byte[]), o => q($"data:application/octet-stream;base64, {Convert.ToBase64String((byte[])o)}"));
        _formatters.Add(typeof(bool), o => (bool)o ? "true" : "false");
        _formatters.Add(typeof(byte), o => o.ToString());
        _formatters.Add(typeof(char), o => q(o));
        _formatters.Add(typeof(decimal), o => q(o));
        _formatters.Add(typeof(float), o => o.ToString());
        _formatters.Add(typeof(int), o => o.ToString());
        _formatters.Add(typeof(long), o => o.ToString());
        _formatters.Add(typeof(sbyte), o => o.ToString());
        _formatters.Add(typeof(short), o => o.ToString());
        _formatters.Add(typeof(string), o => q(o));
        _formatters.Add(typeof(DateTime), o => ((DateTime)o).Ticks.ToString());
        _formatters.Add(typeof(uint), o => o.ToString());
        _formatters.Add(typeof(ulong), o => o.ToString());
        _formatters.Add(typeof(ushort), o => o.ToString());
        _formatters.Add(typeof(Guid), o => q(o));
    }

    /// <summary>
    /// Tries to add a formatter for a spicified type to Json.
    /// </summary>
    /// <param name="type">The type to add a formatter for.</param>
    /// <param name="formater">The type formatter.</param>
    /// <returns></returns>
    public static bool TryAddFormater(Type type, Func<object, string> formater)
    {
        if (_formatters.ContainsKey(type)) return false;
        _formatters.Add(type, formater);
        return true;
    }

    private static string q(object obj) => $"\"{obj}\"";
    internal static string ToJson(object obj, int depth = 1)
    {
        // if record
        if (obj is Record record) return RecordToJson(record, depth - 1);

        // if tuple
        if (IsTuple(obj)) return TupleToJson(obj, depth);

        // if array

        if (_formatters.TryGetValue(obj.GetType(), out Func<object, string> formatter))
            return formatter(obj);

        return q(obj);
    }

    private static string RecordToJson(Record record, int depth)
    {
        StringBuilder sb = new("{");

        List<string> parts = new();

        Type t = record.GetType();

        sb.Append($"{ToJson(nameof(record.Id))}:{ToJson(record.Id)},{ToJson(t.FullName)}:{{");

        PropertyInfo[] properties = t.GetProperties();

        foreach (PropertyInfo property in properties)
            if (JsonAttribute.HasAttribute(property))
            {
                if (IsRecord(property) && depth > 0)
                    parts.Add($"{ToJson(property.Name)}:{ToJson(property.GetValue(record), depth - 1)}");
                else if (IsTuple(property) && TupleObjectAttribute.HasAttribute(property, out string[] labels))
                    parts.Add($"{ToJson(property.Name)}:{TupleObjectToJson(property.GetValue(record), depth, labels)}");
                else
                    parts.Add($"{ToJson(property.Name)}:{ToJson(property.GetValue(record), depth)}");
            }

        return sb.Append($"{string.Join(",", parts)}}}}}").ToString();
    }

    public static string TupleToJson(object obj, int depth)
    {
        StringBuilder sb = new("[");

        List<string> parts = new();

        foreach (object item in ValueTupleValues(obj))
            parts.Add(ToJson(item, depth));

        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    public static string TupleObjectToJson(object obj, int depth, string[] labels)
    {
        StringBuilder sb = new("{");

        List<string> parts = new();

        int i = 0;
        foreach (object item in ValueTupleValues(obj))
        {
            string name = i < labels.Length ? labels[i++] : i++.ToString();
            parts.Add($"{ToJson(name)}:{ToJson(item, depth)}");
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

    private static bool IsRecord(object obj)
    {
        if (obj is PropertyInfo prop) return prop.PropertyType.IsSubclassOf(typeof(Record));
        if (obj is FieldInfo field) return field.FieldType.IsSubclassOf(typeof(Record));
        return obj is Record;
    }
}
