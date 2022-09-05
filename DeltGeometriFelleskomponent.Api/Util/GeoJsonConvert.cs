using System.Reflection;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeltGeometriFelleskomponent.Api.Util;

public static class GeoJsonConvert
{

    public static TType DeserializeObject<TType>(string content)
        => JsonConvert.DeserializeObject<TType>(content, new GeoJsonConverter());
    
    public static string SerializeObject(object content)
        => JsonConvert.SerializeObject(content, new GeoJsonConverter());
}

public class GeoJsonConverter : JsonConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => writer.WriteRawValue(GeoJsonWriter.Write(value));

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        => ReadJsonWithType(objectType, JObject.Load(reader).ToString());

    private static object ReadJsonWithType(Type objectType, string json)
        => GetReaderForType(objectType)
            .Invoke(GeoJsonReader, new object[] { json });

    private static MethodInfo GetReaderForType(Type objectType)
        // ReSharper disable once PossibleNullReferenceException
        => GeoJsonReader
            .GetType()
            .GetMethod("Read", new[] { typeof(string) })
            .MakeGenericMethod(objectType);

    public override bool CanRead => true;

    public override bool CanConvert(Type objectType)
        => SupportedTypes.Contains(objectType);

    private static readonly GeoJsonWriter GeoJsonWriter = new();
    private static readonly GeoJsonReader GeoJsonReader = new();

    private static readonly List<Type> SupportedTypes = new()
    {
        typeof(Feature),
        typeof(FeatureCollection),
        typeof(AttributesTable),
        typeof(Geometry),
        typeof(Point),
        typeof(MultiPoint),
        typeof(LineString),
        typeof(MultiLineString),
        typeof(Polygon),
        typeof(MultiPolygon),
        typeof(GeometryCollection)
    };

}