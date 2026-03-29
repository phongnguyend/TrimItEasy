using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace TrimItEasy.Generators;

[Generator]
public class TrimmerSourceGenerator : IIncrementalGenerator
{
    private const string GeneratedTrimmingAttributeFullName = "TrimItEasy.GeneratedTrimmingAttribute";
    private const string NotTrimmedAttributeFullName = "TrimItEasy.NotTrimmedAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GeneratedTrimmingAttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetMethodInfo(ctx))
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(methodDeclarations, static (spc, methods) => ExecuteMethods(spc, methods!));
    }

    private static MethodToGenerate? GetMethodInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
        {
            return null;
        }

        bool isPartial = methodSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            return null;
        }

        bool hasBody = methodSyntax.Body != null || methodSyntax.ExpressionBody != null;
        if (hasBody)
        {
            return null;
        }

        // Must return void
        if (!methodSymbol.ReturnsVoid)
        {
            return null;
        }

        // Must be static
        if (!methodSymbol.IsStatic)
        {
            return null;
        }

        // Must be an extension method (first parameter has 'this' modifier)
        if (!methodSymbol.IsExtensionMethod || methodSymbol.Parameters.Length == 0)
        {
            return null;
        }

        var targetParam = methodSymbol.Parameters[0];
        var targetType = targetParam.Type as INamedTypeSymbol;
        if (targetType == null)
        {
            return null;
        }

        var allTypes = new Dictionary<string, TypeInfo>();
        CollectTypeGraph(targetType, allTypes);

        var containingType = methodSymbol.ContainingType;
        var containingTypeNames = new List<ContainingTypeInfo>();
        var current = containingType;
        while (current != null)
        {
            var keyword = current.IsValueType ? "struct" : "class";
            if (current.TypeKind == TypeKind.Interface)
            {
                keyword = "interface";
            }

            var modifiers = new List<string>();
            foreach (var syntaxRef in current.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
                {
                    foreach (var mod in typeDecl.Modifiers)
                    {
                        var modText = mod.Text;
                        if (!modifiers.Contains(modText))
                        {
                            modifiers.Add(modText);
                        }
                    }

                    break;
                }
            }

            containingTypeNames.Add(new ContainingTypeInfo(
                current.Name,
                keyword,
                modifiers));

            current = current.ContainingType;
        }

        containingTypeNames.Reverse();

        var ns = containingType.ContainingNamespace.IsGlobalNamespace
            ? null
            : containingType.ContainingNamespace.ToDisplayString();

        var targetTypeFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var paramName = targetParam.Name;

        // Get method accessibility
        var accessibility = methodSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "public"
        };

        return new MethodToGenerate(
            methodSymbol.Name,
            ns,
            containingTypeNames,
            accessibility,
            targetTypeFullName,
            paramName,
            allTypes);
    }

    private static void CollectTypeGraph(INamedTypeSymbol typeSymbol, Dictionary<string, TypeInfo> allTypes)
    {
        var fullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (allTypes.ContainsKey(fullyQualifiedName))
        {
            return;
        }

        var properties = new List<PropertyInfo>();
        var complexPropTypes = new List<INamedTypeSymbol>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop)
            {
                continue;
            }

            if (prop.IsReadOnly || prop.IsWriteOnly || prop.IsStatic || prop.IsIndexer)
            {
                continue;
            }

            if (prop.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (prop.GetMethod is null || prop.SetMethod is null)
            {
                continue;
            }

            bool hasNotTrimmed = prop.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == NotTrimmedAttributeFullName);

            var propType = prop.Type;
            var propTypeKind = ClassifyType(propType);
            var propTypeFullName = propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            string? collectionElementTypeFullName = null;
            PropertyTypeKind? collectionElementTypeKind = null;

            if (propTypeKind == PropertyTypeKind.Collection)
            {
                var elementType = GetCollectionElementType(propType);
                if (elementType != null)
                {
                    collectionElementTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    collectionElementTypeKind = ClassifyType(elementType);

                    if (!hasNotTrimmed && collectionElementTypeKind == PropertyTypeKind.Complex && elementType is INamedTypeSymbol namedElementType)
                    {
                        complexPropTypes.Add(namedElementType);
                    }
                }
            }
            else if (!hasNotTrimmed && propTypeKind == PropertyTypeKind.Complex && propType is INamedTypeSymbol namedPropType)
            {
                complexPropTypes.Add(namedPropType);
            }

            properties.Add(new PropertyInfo(
                prop.Name,
                propTypeFullName,
                propTypeKind,
                hasNotTrimmed,
                collectionElementTypeFullName,
                collectionElementTypeKind));
        }

        allTypes[fullyQualifiedName] = new TypeInfo(fullyQualifiedName, typeSymbol.IsValueType, properties);

        // Recurse into complex property types
        foreach (var namedPropType in complexPropTypes)
        {
            CollectTypeGraph(namedPropType, allTypes);
        }
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol type)
    {
        // Check for IList<T>, ICollection<T>, IEnumerable<T>
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                && iface.TypeArguments.Length == 1)
            {
                return iface.TypeArguments[0];
            }
        }

        // Check if the type itself is IEnumerable<T>
        if (type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
            && namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private static PropertyTypeKind ClassifyType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return PropertyTypeKind.String;
        }

        if (type.SpecialType is SpecialType.System_Boolean
            or SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal
            or SpecialType.System_Char
            or SpecialType.System_DateTime)
        {
            return PropertyTypeKind.Primitive;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return PropertyTypeKind.Primitive;
        }

        // Check if it's a collection (implements IEnumerable<T> but is not string)
        if (IsCollectionType(type))
        {
            return PropertyTypeKind.Collection;
        }

        return PropertyTypeKind.Complex;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return true;
            }
        }

        if (type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return true;
        }

        return false;
    }

    private static void ExecuteMethods(SourceProductionContext context, ImmutableArray<MethodToGenerate?> methods)
    {
        if (methods.IsDefaultOrEmpty)
        {
            return;
        }

        var validMethods = methods.Where(m => m is not null).Cast<MethodToGenerate>().ToList();

        if (validMethods.Count == 0)
        {
            return;
        }

        foreach (var method in validMethods)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            if (method.Namespace != null)
            {
                sb.AppendLine($"namespace {method.Namespace};");
                sb.AppendLine();
            }

            // Open containing types
            var indent = "";
            foreach (var containingType in method.ContainingTypes)
            {
                var modifiers = new List<string>(containingType.Modifiers);
                if (!modifiers.Contains("partial"))
                {
                    modifiers.Add("partial");
                }

                var modifiersStr = string.Join(" ", modifiers);
                sb.AppendLine($"{indent}{modifiersStr} {containingType.Keyword} {containingType.Name}");
                sb.AppendLine($"{indent}{{");
                indent += "    ";
            }

            // Generate the partial method implementation
            sb.AppendLine($"{indent}{method.Accessibility} static partial void {method.MethodName}(this {method.TargetTypeFullName} {method.ParameterName})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);");
            sb.AppendLine($"{indent}    TrimProperties_{EscapeTypeName(method.TargetTypeFullName)}({method.ParameterName}, visited);");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Generate per-type trim helper methods
            foreach (var kvp in method.AllTypes)
            {
                GenerateTypeTrimHelper(sb, indent, kvp.Value, method.AllTypes);
            }

            // Generate the recurse helper for collections and unknown complex types
            GenerateRecurseHelper(sb, indent, method.AllTypes);

            // Close containing types
            for (int i = method.ContainingTypes.Count - 1; i >= 0; i--)
            {
                indent = new string(' ', i * 4);
                sb.AppendLine($"{indent}}}");
            }

            var fileName = $"{string.Join("_", method.ContainingTypes.Select(c => c.Name))}_{method.MethodName}.g.cs";
            context.AddSource(fileName, sb.ToString());
        }
    }

    private static void GenerateTypeTrimHelper(StringBuilder sb, string indent, TypeInfo type, Dictionary<string, TypeInfo> allTypes)
    {
        var escapedName = EscapeTypeName(type.FullyQualifiedName);

        sb.AppendLine($"{indent}private static void TrimProperties_{escapedName}({type.FullyQualifiedName} obj, HashSet<object> visited)");
        sb.AppendLine($"{indent}{{");

        if (!type.IsValueType)
        {
            sb.AppendLine($"{indent}    if (obj == null || !visited.Add(obj))");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        return;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        foreach (var prop in type.Properties)
        {
            if (prop.HasNotTrimmed)
            {
                continue;
            }

            if (prop.TypeKind == PropertyTypeKind.String)
            {
                sb.AppendLine($"{indent}    if (obj.{prop.Name} != null)");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        obj.{prop.Name} = obj.{prop.Name}.Trim();");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine();
            }
            else if (prop.TypeKind == PropertyTypeKind.Complex)
            {
                if (allTypes.ContainsKey(prop.TypeFullName))
                {
                    // Known type — call the specific helper
                    sb.AppendLine($"{indent}    if (obj.{prop.Name} != null)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        TrimProperties_{EscapeTypeName(prop.TypeFullName)}(obj.{prop.Name}, visited);");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                }
                else
                {
                    // Unknown complex type — call the generic recurse helper
                    sb.AppendLine($"{indent}    if (obj.{prop.Name} != null)");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        TrimRecursive(obj.{prop.Name}, visited);");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                }
            }
            else if (prop.TypeKind == PropertyTypeKind.Collection)
            {
                sb.AppendLine($"{indent}    if (obj.{prop.Name} != null)");
                sb.AppendLine($"{indent}    {{");

                if (prop.CollectionElementTypeKind == PropertyTypeKind.String)
                {
                    // List<string> — trim in place via IList
                    sb.AppendLine($"{indent}        if (obj.{prop.Name} is IList list_{prop.Name})");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            for (int i = 0; i < list_{prop.Name}.Count; i++)");
                    sb.AppendLine($"{indent}            {{");
                    sb.AppendLine($"{indent}                if (list_{prop.Name}[i] is string s)");
                    sb.AppendLine($"{indent}                {{");
                    sb.AppendLine($"{indent}                    list_{prop.Name}[i] = s.Trim();");
                    sb.AppendLine($"{indent}                }}");
                    sb.AppendLine($"{indent}            }}");
                    sb.AppendLine($"{indent}        }}");
                }
                else if (prop.CollectionElementTypeKind == PropertyTypeKind.Complex
                         && prop.CollectionElementTypeFullName != null
                         && allTypes.ContainsKey(prop.CollectionElementTypeFullName))
                {
                    // List<KnownType> — iterate and call the specific helper
                    sb.AppendLine($"{indent}        foreach (var item in obj.{prop.Name})");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            if (item != null)");
                    sb.AppendLine($"{indent}            {{");
                    sb.AppendLine($"{indent}                TrimProperties_{EscapeTypeName(prop.CollectionElementTypeFullName)}(item, visited);");
                    sb.AppendLine($"{indent}            }}");
                    sb.AppendLine($"{indent}        }}");
                }
                else if (prop.CollectionElementTypeKind is PropertyTypeKind.Complex or PropertyTypeKind.Collection)
                {
                    // Unknown element type — use generic recurse
                    sb.AppendLine($"{indent}        foreach (var item in obj.{prop.Name})");
                    sb.AppendLine($"{indent}        {{");
                    sb.AppendLine($"{indent}            if (item != null)");
                    sb.AppendLine($"{indent}            {{");
                    sb.AppendLine($"{indent}                TrimRecursive(item, visited);");
                    sb.AppendLine($"{indent}            }}");
                    sb.AppendLine($"{indent}        }}");
                }
                // Primitive collections — nothing to trim

                sb.AppendLine($"{indent}    }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static void GenerateRecurseHelper(StringBuilder sb, string indent, Dictionary<string, TypeInfo> allTypes)
    {
        sb.AppendLine($"{indent}private static void TrimRecursive(object? obj, HashSet<object> visited)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (obj == null || obj is string)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}    var type = obj.GetType();");
        sb.AppendLine($"{indent}    if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(System.DateTime) || type == typeof(System.DateTimeOffset))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}    if (!type.IsValueType && !visited.Add(obj))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Dispatch to known types
        if (allTypes.Count > 0)
        {
            bool first = true;
            foreach (var kvp in allTypes)
            {
                var typeInfo = kvp.Value;
                var keyword = first ? "if" : "else if";
                first = false;
                sb.AppendLine($"{indent}    {keyword} (obj is {typeInfo.FullyQualifiedName} typed_{EscapeTypeName(typeInfo.FullyQualifiedName)})");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        TrimProperties_{EscapeTypeName(typeInfo.FullyQualifiedName)}(typed_{EscapeTypeName(typeInfo.FullyQualifiedName)}, visited);");
                sb.AppendLine($"{indent}        return;");
                sb.AppendLine($"{indent}    }}");
            }
            sb.AppendLine();
        }

        // Handle IList with string elements
        sb.AppendLine($"{indent}    if (obj is IList list)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        for (int i = 0; i < list.Count; i++)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            if (list[i] is string s)");
        sb.AppendLine($"{indent}            {{");
        sb.AppendLine($"{indent}                list[i] = s.Trim();");
        sb.AppendLine($"{indent}            }}");
        sb.AppendLine($"{indent}            else");
        sb.AppendLine($"{indent}            {{");
        sb.AppendLine($"{indent}                TrimRecursive(list[i], visited);");
        sb.AppendLine($"{indent}            }}");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        return;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Handle other IEnumerable
        sb.AppendLine($"{indent}    if (obj is IEnumerable enumerable)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        foreach (var item in enumerable)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            if (item is not string)");
        sb.AppendLine($"{indent}            {{");
        sb.AppendLine($"{indent}                TrimRecursive(item, visited);");
        sb.AppendLine($"{indent}            }}");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        return;");
        sb.AppendLine($"{indent}    }}");

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static string EscapeTypeName(string fullyQualifiedName)
    {
        return fullyQualifiedName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace("+", "_")
            .Replace(" ", "");
    }

    private sealed class MethodToGenerate
    {
        public string MethodName { get; }
        public string? Namespace { get; }
        public List<ContainingTypeInfo> ContainingTypes { get; }
        public string Accessibility { get; }
        public string TargetTypeFullName { get; }
        public string ParameterName { get; }
        public Dictionary<string, TypeInfo> AllTypes { get; }

        public MethodToGenerate(string methodName, string? ns, List<ContainingTypeInfo> containingTypes,
            string accessibility, string targetTypeFullName, string parameterName, Dictionary<string, TypeInfo> allTypes)
        {
            MethodName = methodName;
            Namespace = ns;
            ContainingTypes = containingTypes;
            Accessibility = accessibility;
            TargetTypeFullName = targetTypeFullName;
            ParameterName = parameterName;
            AllTypes = allTypes;
        }
    }

    private sealed class ContainingTypeInfo
    {
        public string Name { get; }
        public string Keyword { get; }
        public List<string> Modifiers { get; }

        public ContainingTypeInfo(string name, string keyword, List<string> modifiers)
        {
            Name = name;
            Keyword = keyword;
            Modifiers = modifiers;
        }
    }

    private sealed class TypeInfo
    {
        public string FullyQualifiedName { get; }
        public bool IsValueType { get; }
        public List<PropertyInfo> Properties { get; }

        public TypeInfo(string fullyQualifiedName, bool isValueType, List<PropertyInfo> properties)
        {
            FullyQualifiedName = fullyQualifiedName;
            IsValueType = isValueType;
            Properties = properties;
        }
    }

    private sealed class PropertyInfo
    {
        public string Name { get; }
        public string TypeFullName { get; }
        public PropertyTypeKind TypeKind { get; }
        public bool HasNotTrimmed { get; }
        public string? CollectionElementTypeFullName { get; }
        public PropertyTypeKind? CollectionElementTypeKind { get; }

        public PropertyInfo(string name, string typeFullName, PropertyTypeKind typeKind, bool hasNotTrimmed,
            string? collectionElementTypeFullName = null, PropertyTypeKind? collectionElementTypeKind = null)
        {
            Name = name;
            TypeFullName = typeFullName;
            TypeKind = typeKind;
            HasNotTrimmed = hasNotTrimmed;
            CollectionElementTypeFullName = collectionElementTypeFullName;
            CollectionElementTypeKind = collectionElementTypeKind;
        }
    }

    private enum PropertyTypeKind
    {
        String,
        Primitive,
        Complex,
        Collection
    }
}
