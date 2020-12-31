using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumExtension
{
    public const string PreRelease = "pre-release";
    public const string Semantic = "semantic";

    public static IReadOnlyCollection<TEnumAttribute>
        GetEnumAttributes<TEnum, TEnumAttribute>(
            Func<TEnumAttribute, bool> filterAttribute)
        where TEnum : struct, Enum
        where TEnumAttribute : Attribute
    {
        var enumAttributes = Enums.GetMembers<TEnum>()
            .Select(tEnum => tEnum.Attributes.Get<TEnumAttribute>())
            .Where(filterAttribute)
            .ToList()
            .AsReadOnly();

        return enumAttributes;
    }

    public static bool ContainEnumAttribute<TEnum, TEnumAttribute>(
            Func<TEnumAttribute, bool> filterAttribute)
        where TEnum : struct, Enum
        where TEnumAttribute : Attribute
    {
        var any = Enums.GetMembers<TEnum>()
            .Select(tEnum => tEnum.Attributes.Get<TEnumAttribute>())
            .Where(filterAttribute)
            .Any();

        return any;
    }

    public static bool IsEnumValid<TEnum>(string bumpVersion)
        where TEnum : struct, Enum
    {
        if(String.IsNullOrWhiteSpace(bumpVersion))
        {
            return false;
        }

        var isValid = Enums.GetMembers<TEnum>()
            .Any(tEnum => String.Equals(
                bumpVersion
                , tEnum.AsString()
                , StringComparison.InvariantCultureIgnoreCase));

        return isValid;
    }

    public static TEnum ParseDisplayName<TEnum>(string enumDisplayName)
        where TEnum : struct, Enum
    {
        var tEnum = Enums.Parse<TEnum>(enumDisplayName
            , ignoreCase: true
            , EnumFormat.DisplayName);

        return tEnum;
    }

    public static string ToEnumMembersString<TEnum>(string separator = ", ")
        where TEnum : struct, Enum
    {
        return String.Join(separator: separator
            , Enums.GetMembers<TEnum>()
            .Select(tEnum => tEnum.AsString()));
    }
}