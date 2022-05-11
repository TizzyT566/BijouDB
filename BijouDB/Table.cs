﻿using System.Runtime.CompilerServices;

namespace BijouDB;

public abstract partial class Table
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;
    public bool OnDisk => Id != Guid.Empty;

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static bool TryGet<T>(Guid id, out T? record) where T : Table, new()
    {
        try
        {
            using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{id}.{Globals.Rec}"), FileMode.Open, FileAccess.Read, FileShare.Read);
            record = new();
            record._guid = id;
            return true;
        }
        catch (Exception ex)
        {
            if (Globals.Logging) Console.WriteLine(ex.ToString());
            record = null;
            return false;
        }
    }

    public static IReadOnlySet<Guid> RecordsWithValues(params IReadOnlySet<Guid>[] columnMatches)
    {
        if (columnMatches.Length == 1) return columnMatches[0];
        HashSet<Guid> result = new();
        if (columnMatches.Length == 0) return result;
        Array.Sort(columnMatches, (x, y) => x.Count - y.Count);
        foreach (Guid id in columnMatches[0])
        {
            int i = 1;
            for (; i < columnMatches.Length; i++)
                if (!columnMatches[i].Contains(id))
                    break;
            if (i == columnMatches.Length)
                result.Add(id);
        }
        return result;
    }

    public D Get<D>(D @default = default!, [CallerMemberName] string memberName = "") where D : IDataType
    {
        Type type = GetType();

        return @default;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Column : Attribute
    {
        [Flags]
        public enum Attributes
        {
            None = 0,
            Indexed = 1,
            Unique = 3,

        }

        internal readonly Attributes _attributes;

        public Column(Attributes attributes = Attributes.None) => _attributes = attributes;
    }

    public void Set<D>(D value, [CallerMemberName] string memberName = "") where D : IDataType
    {
        Type type = GetType();

    }

    public T[] References<T>() where T : Table
    {
        Type type = GetType();

        return default;
    }
}