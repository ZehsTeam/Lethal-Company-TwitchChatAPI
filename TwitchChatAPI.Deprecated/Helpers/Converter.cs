using Newtonsoft.Json;
using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Helpers;

internal static class Converter
{
    public static T Convert<T>(object source, T defaultValue) where T : new()
    {
        if (TryConvert(source, out T result))
        {
            return result;
        }

        return defaultValue;
    }

    public static bool TryConvert<T>(object source, out T result) where T : new()
    {
        result = default;

        if (source == null)
        {
            return false;
        }

        try
        {
            string data = JsonConvert.SerializeObject(source);
            result = JsonConvert.DeserializeObject<T>(data);

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public static IEnumerable<T> ConvertList<T>(IEnumerable<object> items) where T : new()
    {
        return ConvertList<object, T>(items);
    }

    public static IEnumerable<TTarget> ConvertList<TSource, TTarget>(IEnumerable<TSource> items) where TTarget : new()
    {
        var list = new List<TTarget>();

        foreach (var item in items)
        {
            if (TryConvert(item, out TTarget result))
            {
                list.Add(result);
            }
        }

        return list;
    }
}
