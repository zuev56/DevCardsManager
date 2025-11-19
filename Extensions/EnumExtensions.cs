using System;
using System.ComponentModel;
using System.Linq;

namespace DevCardsManager.Extensions;

internal static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString())!;
        var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .ToList();

        return attributes is { Count: > 0 }
            ? attributes[0].Description
            : value.ToString();
    }
}