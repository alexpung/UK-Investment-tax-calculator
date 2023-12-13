using System.ComponentModel;
using System.Reflection;
namespace Enum2;
public static class EnumExtensions
{
    /// <summary>
    /// Get human friendly description of the Enum type
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescription(this System.Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        return Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is not DescriptionAttribute attribute
            ? value.ToString()
            : attribute.Description;
    }
}
