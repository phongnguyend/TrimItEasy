namespace TrimItEasy;

/// <summary>
/// Attribute to mark properties that should not be trimmed by the TrimmingExtensions.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotTrimmedAttribute : Attribute
{
} 