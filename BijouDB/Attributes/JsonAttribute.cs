using System.Reflection;

namespace BijouDB;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JsonAttribute : Attribute
{
    public static bool HasAttribute(PropertyInfo propertyInfo) =>
        propertyInfo.GetCustomAttributes<JsonAttribute>().Any();
}
