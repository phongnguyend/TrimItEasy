using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
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

            var value = GetValue(obj, prop);
            if (value == null)
            {
                continue;
            }

            if (prop.PropertyType == typeof(string))
            {
                SetValue(obj, prop, ((string)value).Trim());
            }
            else if (options.Recursive && currentDepth < options.MaxDepth)
            {
                TrimStringsRecursive(value, visited, options, currentDepth + 1);
            }
        }
    }

    private static object? GetValue(object obj, PropertyInfo prop)
    {
        return GetValueUsingDelegate(obj, prop);
    }

    private static void SetValue(object obj, PropertyInfo prop, string value)
    {
        SetValueUsingDelegate(obj, prop, value);
    }

    private static object? GetValueUsingReflection(object obj, PropertyInfo prop)
    {
        return prop.GetValue(obj);
    }

    private static void SetValueUsingReflection(object obj, PropertyInfo prop, string value)
    {
        prop.SetValue(obj, value);
    }

    private static object? GetValueUsingDelegate(object obj, PropertyInfo prop)
    {
        var key = (obj.GetType(), prop.Name);
        var getter = _getterCache.GetOrAdd(key, _ =>
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, obj.GetType());
            var propertyAccess = Expression.Property(castInstance, prop);
            var castResult = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object, object?>>(castResult, instanceParam).Compile();
        });

        return getter(obj);
    }

    private static void SetValueUsingDelegate(object obj, PropertyInfo prop, string value)
    {
        var key = (obj.GetType(), prop.Name);
        var setter = _setterCache.GetOrAdd(key, _ =>
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var castInstance = Expression.Convert(instanceParam, obj.GetType());
            var castValue = Expression.Convert(valueParam, prop.PropertyType);
            var propertyAccess = Expression.Property(castInstance, prop);
            var assign = Expression.Assign(propertyAccess, castValue);
            return Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile();
        });

        setter(obj, value);
    }

    private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>> _getterCache = new();
    private static readonly ConcurrentDictionary<(Type, string), Action<object, string>> _setterCache = new();

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
