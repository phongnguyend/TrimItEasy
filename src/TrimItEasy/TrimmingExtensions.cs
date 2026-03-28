using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrimItEasy;

public static partial class TrimmingExtensions
{
    private static readonly TrimmingOptions _defaultOptions = new TrimmingOptions();

    public static void TrimStrings(this object obj, TrimmingOptions? options = null)
    {
        if (obj == null)
        {
            return;
        }

        options ??= _defaultOptions;

        var type = obj.GetType();
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
        {
            throw new ArgumentException("TrimStrings expects a complex object with properties, not a primitive or string.");
        }

        var visited = new HashSet<object>(new ReferenceEqualityComparer());

        TrimStringsRecursive(obj, visited, options, currentDepth: 0);
    }

    private static void TrimStringsRecursive(object? obj, HashSet<object> visited, TrimmingOptions options, int currentDepth)
    {
        if (obj == null || obj is string)
        {
            return;
        }

        var type = obj.GetType();
        if (type.IsPrimitive
            || type.IsEnum
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset))
        {
            return; // Skip primitives, enums, decimal, DateTime, DateTimeOffset
        }

        if (!type.IsValueType && visited.Contains(obj))
        {
            return;
        }

        if (!type.IsValueType)
        {
            visited.Add(obj);
        }

        if (obj is IEnumerable enumerable && obj is not string)
        {
            if (!options.Recursive)
            {
                return;
            }

            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is string str)
                    {
                        list[i] = str?.Trim();
                    }
                    else
                    {
                        TrimStringsRecursive(list[i], visited, options, currentDepth);
                    }
                }
            }
            else
            {
                foreach (var item in enumerable)
                {
                    if (item is string)
                    {
                        continue; // can't replace in non-indexable enumerable
                    }

                    TrimStringsRecursive(item, visited, options, currentDepth);
                }
            }

            return;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

            // Skip properties marked with NotTrimmed attribute
            if (prop.GetCustomAttribute<NotTrimmedAttribute>() != null)
            {
                continue;
            }

            var value = prop.GetValue(obj);
            if (value == null)
            {
                continue;
            }

            if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(obj, ((string)value).Trim());
            }
            else if (options.Recursive && currentDepth < options.MaxDepth)
            {
                TrimStringsRecursive(value, visited, options, currentDepth + 1);
            }
        }
    }

    private class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

public static partial class TrimmingExtensions
{
    public static T? TrimText<T>(this T? obj)
    {
        if (obj == null)
        {
            return obj;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        options.Converters.Add(new TrimmingStringJsonConverter());

        var json = JsonSerializer.Serialize(obj, options);

        return JsonSerializer.Deserialize<T>(json, options);
    }
}
