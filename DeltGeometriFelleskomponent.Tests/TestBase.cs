using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeltGeometriFelleskomponent.Api.Util;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Features;

namespace DeltGeometriFelleskomponent.Tests;

public abstract class TestBase
{
    protected static NgisFeature ReadFeature(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<NgisFeature>(text);
    }

    protected static NgisFeature GetExampleFeature(string id)
    {

        var features = ReadFeatureCollection("Examples/example_geometries.geojson");
        var feature = features.FirstOrDefault(f => (string)f.Attributes["id"] == id);
        if (feature != null)
        {
            return NgisFeatureHelper.CreateFeature(feature.Geometry, id);
        }
        throw new Exception($"Feature with id={id} not found");
    }

    protected static FeatureCollection ReadFeatureCollection(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<FeatureCollection>(text);
    }

    protected static CreatePolygonFromLinesRequest ReadCreatePolygonFromLinesRequest(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<CreatePolygonFromLinesRequest>(text);
    }


}