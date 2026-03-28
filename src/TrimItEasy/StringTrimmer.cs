using System.Collections;

namespace TrimItEasy;

internal abstract class StringTrimmer
{
    private static readonly TrimmingOptions DefaultOptions = new();

    public void Trim(object obj, TrimmingOptions? options = null)
    {
        if (obj == null)
        {
            return;
        }

        options ??= DefaultOptions;

        var type = obj.GetType();
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
        {
            throw new ArgumentException("TrimStrings expects a complex object with properties, not a primitive or string.");
        }

        var visited = new HashSet<object>(new ReferenceEqualityComparer());

        TrimRecursive(obj, visited, options, currentDepth: 0);
    }

    private void TrimRecursive(object? obj, HashSet<object> visited, TrimmingOptions options, int currentDepth)
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
            return;
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
                        TrimRecursive(list[i], visited, options, currentDepth);
                    }
                }
            }
            else
            {
                foreach (var item in enumerable)
                {
                    if (item is string)
                    {
                        continue;
                    }

                    TrimRecursive(item, visited, options, currentDepth);
                }
            }

            return;
        }

        TrimProperties(obj, type, visited, options, currentDepth);
    }

    protected Action<object?, HashSet<object>, TrimmingOptions, int> GetRecurseAction() => TrimRecursive;

    protected abstract void TrimProperties(object obj, Type type, HashSet<object> visited, TrimmingOptions options, int currentDepth);

    private class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
