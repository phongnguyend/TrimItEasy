using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace TrimItEasy;

internal sealed class DelegateStringTrimmer : StringTrimmer
{
    private delegate void TrimDelegate(object obj, HashSet<object> visited, TrimmingOptions options, int currentDepth,
        Action<object?, HashSet<object>, TrimmingOptions, int> recurse);

    private static readonly ConcurrentDictionary<Type, TrimDelegate> TrimDelegateCache = new();

    protected override void TrimProperties(object obj, Type type, HashSet<object> visited, TrimmingOptions options, int currentDepth)
    {
        var trimDelegate = TrimDelegateCache.GetOrAdd(type, BuildTrimDelegate);
        trimDelegate(obj, visited, options, currentDepth, GetRecurseAction());
    }

    private static TrimDelegate BuildTrimDelegate(Type type)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var visitedParam = Expression.Parameter(typeof(HashSet<object>), "visited");
        var optionsParam = Expression.Parameter(typeof(TrimmingOptions), "options");
        var depthParam = Expression.Parameter(typeof(int), "currentDepth");
        var recurseParam = Expression.Parameter(
            typeof(Action<object?, HashSet<object>, TrimmingOptions, int>), "recurse");

        var typed = Expression.Variable(type, "typed");
        var assignTyped = Expression.Assign(typed, Expression.Convert(objParam, type));

        var statements = new List<Expression> { assignTyped };

        var trimMethod = typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!;
        var recursiveProperty = typeof(TrimmingOptions).GetProperty(nameof(TrimmingOptions.Recursive))!;
        var maxDepthProperty = typeof(TrimmingOptions).GetProperty(nameof(TrimmingOptions.MaxDepth))!;

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

            var propAccess = Expression.Property(typed, prop);

            if (prop.PropertyType == typeof(string))
            {
                var trimmed = Expression.Call(propAccess, trimMethod);
                var assignTrimmed = Expression.Assign(Expression.Property(typed, prop), trimmed);
                var nullCheck = Expression.IfThen(
                    Expression.NotEqual(propAccess, Expression.Constant(null, typeof(string))),
                    assignTrimmed);
                statements.Add(nullCheck);
            }
            else
            {
                var propValue = prop.PropertyType.IsValueType
                    ? (Expression)Expression.Convert(propAccess, typeof(object))
                    : propAccess;

                var invokeRecurse = Expression.Invoke(recurseParam,
                    propValue,
                    visitedParam,
                    optionsParam,
                    Expression.Add(depthParam, Expression.Constant(1)));

                Expression condition = Expression.AndAlso(
                    Expression.Property(optionsParam, recursiveProperty),
                    Expression.LessThan(depthParam, Expression.Property(optionsParam, maxDepthProperty)));

                if (!prop.PropertyType.IsValueType)
                {
                    condition = Expression.AndAlso(condition,
                        Expression.NotEqual(propAccess, Expression.Constant(null, prop.PropertyType)));
                }

                statements.Add(Expression.IfThen(condition, invokeRecurse));
            }
        }

        var body = Expression.Block(new[] { typed }, statements);

        return Expression.Lambda<TrimDelegate>(body, objParam, visitedParam, optionsParam, depthParam, recurseParam)
            .Compile();
    }
}
