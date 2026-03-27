namespace TrimItEasy;

/// <summary>
/// Options for controlling the behavior of the TrimStrings method.
/// </summary>
public class TrimmingOptions
{
    /// <summary>
    /// Gets or sets whether to trim strings recursively in nested objects.
    /// Default is <c>true</c>.
    /// </summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum depth for recursive trimming.
    /// A value of <c>0</c> means only the top-level properties are trimmed.
    /// A value of <c>1</c> means the top-level and one level of nested objects, and so on.
    /// Default is <c>64</c>.
    /// </summary>
    public int MaxDepth { get; set; } = 64;
}
