using System.Reflection;

namespace BijouDB;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JsonAttribute : Attribute
{
    private int _level;

    public JsonAttribute(int level = 0) => _level = level;

    public static bool HasAttribute(PropertyInfo propertyInfo, out int level)
    {
        bool ret = propertyInfo.GetCustomAttributes<JsonAttribute>().Any();
        if (ret)
        {
            JsonAttribute attr = propertyInfo.GetCustomAttributes<JsonAttribute>().First();
            level = attr._level;
            return true;
        }
        level = -1;
        return false;
    }
}
