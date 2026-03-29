namespace TrimItEasy;

/// <summary>
/// Marks a partial extension method for source-generated string trimming.
/// When applied, the source generator will implement the method body with
/// optimized trimming code for the parameter type's properties, avoiding reflection at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GeneratedTrimmingAttribute : Attribute
{
}
