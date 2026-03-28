using System.Reflection;

namespace TrimItEasy;

internal sealed class ReflectionStringTrimmer : StringTrimmer
{
    protected override void TrimProperties(object obj, Type type, HashSet<object> visited, TrimmingOptions options, int currentDepth)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

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
                GetRecurseAction()(value, visited, options, currentDepth + 1);
            }
        }
    }
}
