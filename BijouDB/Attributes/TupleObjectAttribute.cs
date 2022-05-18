using System.Reflection;

namespace BijouDB;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TupleObjectAttribute : Attribute
{
    public string[] Labels { get; }

    public TupleObjectAttribute(string label, params string[] labels)
    {
        List<string> list = new()
        {
            string.IsNullOrEmpty(label) ? "0" : label
        };

        if (labels is not null)
            for (int i = 0; i < labels.Length;)
            {
                string l = labels[i++];
                list.Add(string.IsNullOrEmpty(l) ? $"{i}" : l);
            }

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