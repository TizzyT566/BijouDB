﻿using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BijouDB;

public static class Json
{
    private static readonly Dictionary<Type, Func<object, string>> _formatters = new();
    private static readonly HashSet<Guid> _references = new();
    private static int _level, _depth, _lock;

    static Json()
    {
        _formatters.Add(typeof(BigInteger), o => $"{o}n");
        _formatters.Add(typeof(byte[]), o => $"\"{Convert.ToBase64String((byte[])o)}\"");
        _formatters.Add(typeof(bool), o => (bool)o ? "true" : "false");
        _formatters.Add(typeof(byte), o => o.ToString());
        _formatters.Add(typeof(char), o => $"\"{o}\"");
        _formatters.Add(typeof(decimal), o => $"\"{o}\"");
        _formatters.Add(typeof(double), o => o.ToString());
        _formatters.Add(typeof(float), o => o.ToString());
        _formatters.Add(typeof(int), o => o.ToString());
        _formatters.Add(typeof(long), o => o.ToString());
        _formatters.Add(typeof(sbyte), o => o.ToString());
        _formatters.Add(typeof(short), o => o.ToString());
        _formatters.Add(typeof(string), o => ProcessString(o));
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
        try
        {
            SpinWait.SpinUntil(() => Interlocked.Exchange(ref _lock, 1) == 0);
            if (_formatters.ContainsKey(type)) return false;
            _formatters.Add(type, formatter);
            return true;
        }
        catch (Exception ex)
        {
            ex.Log();
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _lock, 0);
        }
    }

    /// <summary>
    /// Gets the Json representation of a record give its type and id as strings.
    /// </summary>
    /// <param name="type">The Type name.</param>
    /// <param name="id">The record's Id.</param>
    /// <param name="depth">How deep should the serializer go.</param>
    /// <param name="level">The access level of properties to represent.</param>
    /// <returns>The Json string of the specified record.</returns>
    public static string? GetRecord(string type, string id, int depth = 0, int level = 0)
    {
        try
        {
            Type t = Record._types[type];
            ConstructorInfo ci = t.GetConstructor(Array.Empty<Type>());
            dynamic obj = ci.Invoke(null);
            obj._id = Guid.Parse(id);
            string json = Json.ToJson(obj, depth, level);
            return json;
        }
        catch (Exception ex)
        {
            ex.Log();
            return null;
        }
    }

    /// <summary>
    /// Gets the Json representation of a record give its type, id, and property name as strings.
    /// </summary>
    /// <param name="type">The Type name.</param>
    /// <param name="id">The record's Id.</param>
    /// <param name="property">The name of the property in the record.</param>
    /// <param name="depth">How deep should the serializer go.</param>
    /// <param name="level">The access level of properties to represent.</param>
    /// <returns>The Json string of the specified record.</returns>
    public static string? GetProperty(string type, string id, string property, int depth = 0, int level = 0)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(property)) return null;
        try
        {
            Type t = Record._types[type];
            ConstructorInfo ci = t.GetConstructor(Array.Empty<Type>());
            dynamic obj = ci.Invoke(null);
            obj._id = Guid.Parse(id);
            string prop = Json.ToJson(t.GetProperty(property).GetValue(obj), depth, level);
            return prop;
        }
        catch (Exception ex)
        {
            ex.Log();
            return null;
        }
    }

    /// <summary>
    /// Gets the Json representation of an object.
    /// </summary>
    /// <param name="this">The object to represent as Json.</param>
    /// <param name="depth">How deep should the serializer go.</param>
    /// <param name="level">The access level of properties to represent.</param>
    /// <returns>The Json string of the specified object.</returns>
    public static string ToJson(this object @this, int depth = 1, int level = 0)
    {
        try
        {
            SpinWait.SpinUntil(() => Interlocked.Exchange(ref _lock, 1) == 0);
            _level = level;
            _depth = depth;
            _references.Clear();
            return ToJson(@this);
        }
        catch (Exception ex)
        {
            ex.Log();
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

        if (obj is Record record) return RecordToJson(record);

        if (IsJunction(obj)) return JunctionToJson(obj);

        if (_formatters.TryGetValue(obj.GetType(), out Func<object, string> formatter)) return formatter(obj);

        if (obj.GetType().IsArray) return ArrayToJson(obj);

        if (obj is IEnumerable) return EnumerableToJson(obj);

        if (IsTuple(obj)) return TupleToJson(obj);

        return $"\"{obj}\"";
    }

    private static string ProcessString(object obj)
    {
        string input = (string)obj;
        char[] chars = new char[2 + input.Length * 2];
        chars[0] = '\"';
        int i = 1;
        foreach (char c in input)
        {
            switch (c)
            {
                case '\"':
                    {
                        chars[i++] = '\\';
                        chars[i++] = '\"';
                        break;
                    }
                case '\\':
                    {
                        chars[i++] = '\\';
                        chars[i++] = '\\';
                        break;
                    }
                case '/':
                    {
                        chars[i++] = '\\';
                        chars[i++] = '/';
                        break;
                    }
                case '\b':
                    {
                        chars[i++] = '\\';
                        chars[i++] = 'b';
                        break;
                    }
                case '\f':
                    {
                        chars[i++] = '\\';
                        chars[i++] = 'f';
                        break;
                    }
                case '\n':
                    {
                        chars[i++] = '\\';
                        chars[i++] = 'n';
                        break;
                    }
                case '\r':
                    {
                        chars[i++] = '\\';
                        chars[i++] = 'r';
                        break;
                    }
                case '\t':
                    {
                        chars[i++] = '\\';
                        chars[i++] = 't';
                        break;
                    }
                default:
                    {
                        chars[i++] = c;
                        break;
                    }
            }
        }
        chars[i++] = '\"';
        return new string(chars, 0, i);
    }

    private static string JunctionToJson(object obj)
    {
        dynamic objDyn = obj;
        StringBuilder sb = new("[");
        List<string> parts = new();
        foreach (object item in objDyn.All) parts.Add(ToJson(item));
        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string ArrayToJson(object obj)
    {
        object[] arr = (object[])obj;
        StringBuilder sb = new("[");
        List<string> parts = new();
        foreach (object item in arr) parts.Add(ToJson(item));
        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string EnumerableToJson(object obj)
    {
        StringBuilder sb = new("[");
        List<string> parts = new();
        foreach (object item in (IEnumerable<object>)obj)
            parts.Add(ToJson(item));
        return sb.Append($"{string.Join(",", parts)}]").ToString();
    }

    private static string RecordToJson(Record record)
    {
        StringBuilder sb = new("{");
        Type t = record.GetType();

        sb.Append($"\"{nameof(record.Id)}\":\"{record.Id}\",\"{t.FullName}\":{{");

        List<string> parts = new();

        if (_depth > 0 && !_references.Contains(record.Id))
        {
            Interlocked.Decrement(ref _depth);
            _references.Add(record.Id);

            PropertyInfo[] properties = t.GetProperties();
            foreach (PropertyInfo property in properties)
                if (JsonAttribute.HasAttribute(property, out int level))
                {
                    if (level > _level) continue;
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
