using System.Reflection;

namespace BijouDB;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JsonAttribute : Attribute
{
    public bool _verbose;

    public JsonAttribute(bool verbose = false) => _verbose = verbose;

    public static bool HasAttribute(PropertyInfo propertyInfo, out bool verbose)
    {
        bool ret = propertyInfo.GetCustomAttributes<JsonAttribute>().Any();
        if (ret)
        {
            JsonAttribute attr = propertyInfo.GetCustomAttributes<JsonAttribute>().First();
            verbose = attr._verbose;
            return true;
        }
        verbose = false;
        return false;
    }
}
