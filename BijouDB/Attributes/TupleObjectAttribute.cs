using System.Reflection;

namespace BijouDB;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TupleObjectAttribute : Attribute
{
    public string[] Labels { get; }
    public TupleObjectAttribute(string label, params string[] labels)
    {
        ArgumentException ex = new("Labels cannot be null.");
        List<string> list = new() { label ?? throw ex };
        foreach (string l in labels) list.Add(l ?? throw ex);
        Labels = list.ToArray();
    }
    public static bool HasAttribute(PropertyInfo propertyInfo, out string[] labels)
    {
        bool ret = propertyInfo.GetCustomAttributes<TupleObjectAttribute>().Any();
        if (ret)
        {
            TupleObjectAttribute attr = propertyInfo.GetCustomAttributes<TupleObjectAttribute>().First();
            labels = attr.Labels;
            return true;
        }
        labels = null!;
        return false;
    }
}