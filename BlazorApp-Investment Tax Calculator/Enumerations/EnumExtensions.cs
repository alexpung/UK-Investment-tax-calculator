using System.ComponentModel;
using System.Reflection;
namespace Enumerations;
public static class EnumExtensions
{
    /// <summary>
    /// Get human friendly description of the Enum type
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescription(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        return Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is not DescriptionAttribute attribute
            ? value.ToString()
            : attribute.Description;
    }

    public static List<string> GetDescriptions(Type type)
    {
        List<string> descriptionList = [];
        Array enumValues = Enum.GetValues(type);
        foreach (var enumValue in enumValues)
        {
            descriptionList.Add(((Enum)enumValue).GetDescription());
        }
        return descriptionList;
    }

    public static List<EnumDescriptionPair<T>> GetEnumDescriptionPair<T>(Type type) where T : Enum
    {
        List<EnumDescriptionPair<T>> enumDescriptionPairList = [];
        Array enumValues = Enum.GetValues(type);
        foreach (var enumValue in enumValues)
        {
            T castedEnumValue = (T)enumValue;
            EnumDescriptionPair<T> enumDescriptionPair = new(castedEnumValue, castedEnumValue.GetDescription());
            enumDescriptionPairList.Add(enumDescriptionPair);
        }
        return enumDescriptionPairList;
    }
}

public record EnumDescriptionPair<T>(T EnumValue, string Description) where T : Enum;
