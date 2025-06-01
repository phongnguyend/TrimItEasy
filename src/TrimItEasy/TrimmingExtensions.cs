using System.Collections;
using System.Reflection;

namespace TrimItEasy;

public static class TrimmingExtensions
{
    public static void TrimStrings(this object obj, bool recursive = true)
    {
        if (obj == null)
        {
            return;
        }

        var type = obj.GetType();
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
        {
            throw new ArgumentException("TrimStrings expects a complex object with properties, not a primitive or string.");
        }

        var visited = new HashSet<object>(new ReferenceEqualityComparer());

        TrimStringsRecursive(obj, visited, recursive);
    }

    private static void TrimStringsRecursive(object obj, HashSet<object> visited, bool recursive)
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
            if (!recursive)
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
                        TrimStringsRecursive(list[i], visited, recursive);
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

                    TrimStringsRecursive(item, visited, recursive);
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
            else if (recursive)
            {
                TrimStringsRecursive(value, visited, recursive);
            }
        }
    }

    private class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
