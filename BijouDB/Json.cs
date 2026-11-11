using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace BijouDB;

public static class Json
{
    private static readonly object _formatterLock = new();
    private static readonly Dictionary<Type, Func<object, string>> _formatters = new()
    {
        [typeof(BigInteger)] = o => ((BigInteger)o).ToString(CultureInfo.InvariantCulture),
        [typeof(byte[])] = o => Quote(Convert.ToBase64String((byte[])o)),
        [typeof(bool)] = o => (bool)o ? "true" : "false",
        [typeof(byte)] = InvariantNumber,
        [typeof(char)] = o => Quote(o.ToString()!),
        // Preserve the existing decimal-as-string public representation.
        [typeof(decimal)] = o => Quote(((decimal)o).ToString(CultureInfo.InvariantCulture)),
        [typeof(double)] = o => FormatDouble((double)o),
        [typeof(float)] = o => FormatSingle((float)o),
        [typeof(int)] = InvariantNumber,
        [typeof(long)] = InvariantNumber,
        [typeof(sbyte)] = InvariantNumber,
        [typeof(short)] = InvariantNumber,
        [typeof(string)] = o => Quote((string)o),
        [typeof(DateTime)] = o => ((DateTime)o).Ticks.ToString(CultureInfo.InvariantCulture),
        [typeof(uint)] = InvariantNumber,
        [typeof(ulong)] = InvariantNumber,
        [typeof(ushort)] = InvariantNumber,
        [typeof(Guid)] = o => Quote(o.ToString()!)
    };

    /// <summary>
    /// Adds a formatter. The callback must return a valid JSON value.
    /// </summary>
    /// <param name="type">The type to add a formatter for.</param>
    /// <param name="formatter">The type formatter.</param>
    /// <returns>true if formatter was successfully added, otherwise false.</returns>
    public static bool TryAddFormatter(Type type, Func<object, string> formatter)
    {
        if (type is null || formatter is null || typeof(Record).IsAssignableFrom(type)) return false;

        lock (_formatterLock)
        {
            if (_formatters.ContainsKey(type)) return false;
            _formatters.Add(type, formatter);
            return true;
        }
    }

    /// <summary>
    /// Retrieves and serializes an existing record without running its constructor.
    /// </summary>
    /// <param name="type">The Type name.</param>
    /// <param name="id">The record's Id.</param>
    /// <param name="depth">How deep should the serializer go.</param>
    /// <param name="level">The access level of properties to represent.</param>
    /// <returns>The Json string of the specified record.</returns>
    public static string? GetRecord(string type, string id, int depth = 0, int level = 0)
    {
        if (string.IsNullOrEmpty(type) || !Guid.TryParse(id, out Guid guid) || !Record.TryResolveType(type, out Type? recordType) || recordType is null) return null;
        return Record.TryGet(recordType, guid, out Record? record) ? ToJson(record, depth, level) : null;
    }

    /// <summary>
    /// Retrieves and serializes a public property of an existing record.
    /// </summary>
    /// <param name="type">The Type name.</param>
    /// <param name="id">The record's Id.</param>
    /// <param name="property">The name of the property in the record.</param>
    /// <param name="depth">How deep should the serializer go.</param>
    /// <param name="level">The access level of properties to represent.</param>
    /// <returns>The Json string of the specified record.</returns>
    public static string? GetProperty(string type, string id, string property, int depth = 0, int level = 0)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(property) || !Guid.TryParse(id, out Guid guid) || !Record.TryResolveType(type, out Type? recordType) || recordType is null || !Record.TryGet(recordType, guid, out Record? record))
            return null;

        PropertyInfo? info = recordType.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
        if (info?.GetMethod is null || !info.GetMethod.IsPublic || info.GetIndexParameters().Length != 0) return null;
        if (JsonAttribute.HasAttribute(info, out int propertyLevel) && propertyLevel > level) return null;
        return ToJson(info.GetValue(record), depth, level);
    }

    /// <summary>
    /// Serializes using per-call traversal state. Unsupported values or failing getters/formatters
    /// throw instead of being silently replaced with null. Custom formatters can call ToJson.
    /// </summary>
    public static string ToJson(this object? @this, int depth = 1, int level = 0)
    {
        if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));

        Dictionary<Type, Func<object, string>> formatters;

        lock (_formatterLock)
            formatters = new(_formatters);
        return new SerializationContext(formatters, level).Write(@this, depth);
    }

    private static string InvariantNumber(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Non-finite numbers cannot be represented as JSON numbers.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string FormatSingle(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("Non-finite numbers cannot be represented as JSON numbers.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Quote(string input)
    {
        StringBuilder result = new();
        result.Append('"');
        foreach (char c in input)
        {
            switch (c)
            {
                case '"':
                    result.Append("\\\"");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                default:
                    if (c < ' ' || char.IsSurrogate(c))
                        result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        result.Append(c);
                    break;
            }
        }

        return result.Append('"').ToString();
    }

    private sealed class SerializationContext
    {
        private readonly Dictionary<Type, Func<object, string>> _formatters;
        private readonly int _level;
        private readonly HashSet<(Type, Guid)> _records = new();
        private readonly HashSet<object> _collections = new(ReferenceComparer.Instance);

        internal SerializationContext(Dictionary<Type, Func<object, string>> formatters, int level)
        {
            _formatters = formatters;
            _level = level;
        }

        internal string Write(object? value, int depth)
        {
            if (value is null) return "null";
            if (value is Record record) return WriteRecord(record, depth);

            Type type = value.GetType();
            if (_formatters.TryGetValue(type, out Func<object, string>? formatter))
                return formatter(value) ?? throw new InvalidOperationException("A JSON formatter returned null instead of a JSON value.");
            if (IsJunction(type)) return WriteCollection((IEnumerable)type.GetProperty("All")!.GetValue(value)!, depth);
            if (value is IEnumerable enumerable) return WriteCollection(enumerable, depth);
            if (IsTuple(type)) return WriteTuple(value, depth, null);
            return Quote(value.ToString() ?? "");
        }

        private string WriteRecord(Record record, int depth)
        {
            Type type = record.GetType();
            Guid id = record.Id;
            List<string> parts = new();
            (Type type, Guid id) key = (type, id);
            if (depth > 0 && _records.Add(key))
            {
                try
                {
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (property.GetMethod is null || !property.GetMethod.IsPublic || property.GetIndexParameters().Length != 0 || !JsonAttribute.HasAttribute(property, out int level) || level > _level)
                            continue;

                        object? value = property.GetValue(record);
                        string json = value is not null && IsTuple(value.GetType()) && TupleObjectAttribute.HasAttribute(property, out string[] labels) ? WriteTuple(value, depth - 1, labels) : Write(value, depth - 1);
                        parts.Add(Quote(property.Name) + ":" + json);
                    }
                }
                finally
                {
                    _records.Remove(key);
                }
            }

            return "{" + Quote(nameof(Record.Id)) + ":" + Quote(id.ToString()) + "," + Quote(type.FullName ?? type.Name) + ":{" + string.Join(",", parts) + "}}";
        }

        private string WriteCollection(IEnumerable value, int depth)
        {
            if (!_collections.Add(value)) throw new InvalidOperationException("A collection contains a reference cycle.");

            try
            {
                List<string> parts = new();
                foreach (object? item in value)
                    parts.Add(Write(item, depth));

                return "[" + string.Join(",", parts) + "]";
            }
            finally
            {
                _collections.Remove(value);
            }
        }

        private string WriteTuple(object value, int depth, string[]? labels)
        {
            List<string> parts = new();
            int i = 0;
            foreach (object? item in TupleValues(value))
            {
                string json = Write(item, depth);
                if (labels is not null)
                    json = Quote(i < labels.Length ? labels[i] : i.ToString(CultureInfo.InvariantCulture)) + ":" + json;
                parts.Add(json);
                i++;
            }

            return labels is null ? "[" + string.Join(",", parts) + "]" : "{" + string.Join(",", parts) + "}";
        }
    }

    private static bool IsTuple(Type type) => type.IsGenericType && (type.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true || type.GetGenericTypeDefinition().FullName?.StartsWith("System.Tuple`", StringComparison.Ordinal) == true);

    private static IEnumerable<object?> TupleValues(object tuple)
    {
        Type type = tuple.GetType();
        for (int i = 1; i <= 7; i++)
        {
            string name = "Item" + i;
            FieldInfo? field = type.GetField(name);
            PropertyInfo? property = type.GetProperty(name);
            if (field is null && property is null)
                yield break;
            yield return field is not null ? field.GetValue(tuple) : property!.GetValue(tuple);
        }

        object? rest = type.GetField("Rest")?.GetValue(tuple) ?? type.GetProperty("Rest")?.GetValue(tuple);
        if (rest is not null)
            foreach (object? item in TupleValues(rest))
                yield return item;
    }

    private static bool IsJunction(Type type) => type.IsGenericType && type.GetGenericTypeDefinition().FullName == "BijouDB.Relational`2+Junc";

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new();

        public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);

        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
}
