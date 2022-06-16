using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NJsonSchema.Generation;

namespace DeltGeometriFelleskomponent.Api;

/*
 * For all GeoJSON types we add a link to the spec in description.
 */
public static class GeoJsonOpenApiDefs
{
    public static void AddGeoJsonMappings(JsonSchemaGeneratorSettings config)
    {
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(Geometry), "A GeoJSON Geometry as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(Point), "A GeoJSON Point as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.2"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(MultiPoint), "A GeoJSON MultiPoint as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.3"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(LineString), "A GeoJSON LineString as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.4"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(MultiLineString), "A GeoJSON MultiLineString as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.5"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(Polygon), "A GeoJSON Polygon as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.6"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(MultiPolygon), "A GeoJSON MultiPolygon as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.7"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(GeometryCollection), "A GeoJSON GeometryCollection as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.1.8"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(Feature), "A GeoJSON Feature as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.2"));
        config.TypeMappers.Add(new DescriptionBasedTypeMapper(typeof(FeatureCollection), "A GeoJSON FeatureCollection as specified by https://datatracker.ietf.org/doc/html/rfc7946#section-3.3"));
    }
}