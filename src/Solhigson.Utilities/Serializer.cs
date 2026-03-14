using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Solhigson.Utilities;

public static class Serializer
{
    private static readonly XmlWriterSettings DefaultXmlWriterSettings = new () {OmitXmlDeclaration = true};

    private static readonly XmlSerializerNamespaces DefaultXmlSerializerNamespaces = new([XmlQualifiedName.Empty]);

    internal static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new ()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions =
        new (DefaultJsonSerializerOptions)
        {
            WriteIndented = true,
        };

    // No CamelCase policy — preserves PascalCase property names for form encoding,
    // matching prior Newtonsoft behavior where JObject.FromObject(obj) used defaults.
    private static readonly JsonSerializerOptions KeyValueJsonSerializerOptions = new ()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public static IDictionary<string, string?>? SerializeToKeyValue(this object? obj)
    {
        if (obj is null) return null;

        var node = obj as JsonNode ?? JsonSerializer.SerializeToNode(obj, KeyValueJsonSerializerOptions);
        if (node is null) return null;

        var result = new Dictionary<string, string?>();
        FlattenNode(node, result);
        return result;
    }

    private static void FlattenNode(JsonNode node, Dictionary<string, string?> result)
    {
        switch (node)
        {
            case JsonObject jsonObj:
                foreach (var (_, value) in jsonObj)
                {
                    if (value is not null)
                    {
                        FlattenNode(value, result);
                    }
                }
                break;

            case JsonArray jsonArr:
                foreach (var item in jsonArr)
                {
                    if (item is not null)
                    {
                        FlattenNode(item, result);
                    }
                }
                break;

            case JsonValue jsonVal:
                var path = node.GetPath();
                if (path.StartsWith("$."))
                    path = path[2..];
                else if (path == "$")
                    path = string.Empty;

                string? valueStr;
                if (jsonVal.TryGetValue<JsonElement>(out var element))
                {
                    valueStr = element.ValueKind == JsonValueKind.String
                        ? element.GetString()
                        : element.GetRawText();
                }
                else
                {
                    valueStr = jsonVal.ToString();
                }

                result[path] = valueStr;
                break;
        }
    }

    public static string? SerializeToXml(this object? obj, XmlSerializerNamespaces? xmlsn = null,
        XmlWriterSettings? settings = null)
    {
        if (obj == null) return null;
        xmlsn ??= DefaultXmlSerializerNamespaces;
        settings ??= DefaultXmlWriterSettings;

        var serializer = new XmlSerializer(obj.GetType());

        using var stream = new StringWriter();
        using var writer = XmlWriter.Create(stream, settings);
        serializer.Serialize(writer, obj, xmlsn);
        return stream.ToString();
    }

    public static string? SerializeToXmlUtf8(this object? obj, XmlSerializerNamespaces? xmlsn = null,
        XmlWriterSettings? settings = null)
    {
        if (obj == null) return null;
        xmlsn ??= DefaultXmlSerializerNamespaces;
        settings ??= DefaultXmlWriterSettings;

        var serializer = new XmlSerializer(obj.GetType());

        using var stream = new Utf8StringWriter();
        using var writer = XmlWriter.Create(stream, settings);
        serializer.Serialize(writer, obj, xmlsn);
        return stream.ToString();
    }

    public static string? SerializeToJson(this object? obj, bool indent = false,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (obj is null)
        {
            return null;
        }

        var options = jsonSerializerOptions
            ?? (indent ? IndentedJsonSerializerOptions : DefaultJsonSerializerOptions);

        return JsonSerializer.Serialize(obj, options);
    }

    public static T? DeserializeFromJson<T>(this string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString)) return default;
        return JsonSerializer.Deserialize<T>(jsonString, DefaultJsonSerializerOptions);
    }

    private static object? DeserializeFromXml(this string? xmlString, Type objType)
    {
        if (string.IsNullOrEmpty(xmlString)) return null;

        using var sReader = new StringReader(xmlString);
        using var xmlReader = new XmlTextReader(sReader);
        var xs = new XmlSerializer(objType);
        var theObject = xs.Deserialize(xmlReader);

        return theObject;
    }

    public static T? DeserializeFromXml<T>(this string? xmlString)// where T : class
    {
        return (T?) xmlString.DeserializeFromXml(typeof(T));
    }
}

public class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => new UTF8Encoding(false);
}
