using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrimItEasy.System.Text.Json;

public static class TrimmingExtensions
{
    public static T TrimText<T>(this T obj)
    {
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
