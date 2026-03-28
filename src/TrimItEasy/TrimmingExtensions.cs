using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrimItEasy;

public static partial class TrimmingExtensions
{
    private static readonly DelegateStringTrimmer _delegateTrimmer = new();
    private static readonly ReflectionStringTrimmer _reflectionTrimmer = new();

    public static void TrimStrings(this object obj, TrimmingOptions? options = null)
    {
        _delegateTrimmer.Trim(obj, options);
    }

    internal static void TrimStringsWithReflection(this object obj, TrimmingOptions? options = null)
    {
        _reflectionTrimmer.Trim(obj, options);
    }

    internal static void TrimStringsWithDelegate(this object obj, TrimmingOptions? options = null)
    {
        _delegateTrimmer.Trim(obj, options);
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
