using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace Solhigson.Framework.Utilities;

public static class Serializer
{
    private static readonly XmlWriterSettings DefaultXmlWriterSettings = new () {OmitXmlDeclaration = true};

    private static readonly XmlSerializerNamespaces DefaultXmlSerializerNamespaces = new(new[] {XmlQualifiedName.Empty});
        
    private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new ()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };


    public static IDictionary<string, string> SerializeToKeyValue(this object obj)
    {
        while (true)
        {
            if (obj == null) return null;

            var token = obj as JToken;
            if (token == null)
            {
                obj = JObject.FromObject(obj);
                continue;
            }

            if (token.HasValues)
            {
                var contentData = new Dictionary<string, string>();
                foreach (var child in token.Children().ToList())
                {
                    var childContent = child.SerializeToKeyValue();
                    if (childContent != null)
                        contentData = contentData.Concat(childContent)
                            .ToDictionary(k => k.Key, v => v.Value);
                }

                return contentData;
            }

            var jValue = token as JValue;
            if (jValue?.Value == null) return null;

            var value = jValue?.Type == JTokenType.Date
                ? jValue?.ToString("o", CultureInfo.InvariantCulture)
                : jValue?.ToString(CultureInfo.InvariantCulture);

            return new Dictionary<string, string> {{token.Path, value}};
        }
    }

    public static string SerializeToXml(this object obj, XmlSerializerNamespaces xmlsn = null,
        XmlWriterSettings settings = null)
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

    public static string SerializeToXmlUtf8(this object obj, XmlSerializerNamespaces xmlsn = null,
        XmlWriterSettings settings = null)
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
        
    public static string SerializeToJson(this object obj, bool indent = false, JsonSerializerSettings jsonSerializerSettings = null)
    {
        if (obj == null)
        {
            return null;
        }

        jsonSerializerSettings ??= DefaultJsonSerializerSettings;
        var format = jsonSerializerSettings.Formatting;
        if (indent)
        {
            format = Formatting.Indented;
        }
                
        return JsonConvert.SerializeObject(obj, format, jsonSerializerSettings);
    }
        
    private static object DeserializeFromJson(this string jsonString, Type objType)
    {
        if (string.IsNullOrEmpty(jsonString)) return null;

        using var sReader = new StringReader(jsonString);
        var xs = new JsonSerializer();
        var theObject = xs.Deserialize(sReader, objType);
        return theObject;
    }

    public static T DeserializeFromJson<T>(this string jsonString)
    {
        return (T) DeserializeFromJson(jsonString, typeof(T));
    }

    private static object DeserializeFromXml(this string xmlString, Type objType)
    {
        if (string.IsNullOrEmpty(xmlString)) return null;

        object theObject = null;
        using (var sReader = new StringReader(xmlString))
        {
            using (var xmlReader = new XmlTextReader(sReader))
            {
                var xs = new XmlSerializer(objType);
                theObject = xs.Deserialize(xmlReader);
            }
        }

        return theObject;
    }

    public static T DeserializeFromXml<T>(this string xmlString) where T : class
    {
        return (T) xmlString.DeserializeFromXml(typeof(T));
    }
}

public class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => new UTF8Encoding(false);
}