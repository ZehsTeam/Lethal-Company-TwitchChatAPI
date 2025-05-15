using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.github.zehsteam.TwitchChatAPI.Helpers;

internal static class Converter
{
    public static T Convert<T>(object source) where T : new()
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        T result = new();

        // Get public instance properties and fields
        var otherType = source.GetType();
        var targetType = typeof(T);

        var otherMembers = otherType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);

        foreach (var member in otherMembers)
        {
            Type memberType;
            object value = null;

            if (member is PropertyInfo property && property.CanRead)
            {
                memberType = property.PropertyType;
                value = property.GetValue(source);
            }
            else if (member is FieldInfo field)
            {
                memberType = field.FieldType;
                value = field.GetValue(source);
            }
            else
            {
                continue;
            }

            // Try to find a matching property in target
            var targetProperty = targetType.GetProperty(member.Name, BindingFlags.Public | BindingFlags.Instance);

            if (targetProperty != null && targetProperty.CanWrite && targetProperty.PropertyType == memberType)
            {
                targetProperty.SetValue(result, value);
                continue;
            }

            // Try to find a matching field in target
            var targetField = targetType.GetField(member.Name, BindingFlags.Public | BindingFlags.Instance);

            if (targetField != null && targetField.FieldType == memberType)
            {
                targetField.SetValue(result, value);
            }
        }

        return result;
    }

    public static IEnumerable<T> ConvertList<T>(IEnumerable<object> items) where T : new()
    {
        var result = new List<T>();

        foreach (var item in items)
        {
            result.Add(Convert<T>(item));
        }

        return result;
    }

    public static IEnumerable<TTarget> ConvertList<TSource, TTarget>(IEnumerable<TSource> items) where TTarget : new()
    {
        var result = new List<TTarget>();

        foreach (var item in items)
        {
            result.Add(Convert<TTarget>(item!));
        }

        return result;
    }
}
