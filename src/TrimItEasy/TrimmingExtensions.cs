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

        if (!obj.GetType().IsValueType && visited.Contains(obj))
        {
            return;
        }

        if (!obj.GetType().IsValueType)
        {
            visited.Add(obj);
        }

        if (obj is IEnumerable enumerable && obj is not string)
        {
            if (!recursive)
            {
                return;
            }

            foreach (var item in enumerable)
            {
                TrimStringsRecursive(item, visited, recursive);
            }

            return;
        }

        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
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
            else if (recursive && !prop.PropertyType.IsPrimitive && !prop.PropertyType.IsEnum && prop.PropertyType != typeof(decimal) && prop.PropertyType != typeof(DateTime))
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
