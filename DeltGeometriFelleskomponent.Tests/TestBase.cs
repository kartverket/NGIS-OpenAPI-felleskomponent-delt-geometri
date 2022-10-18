using System;
using System.Collections.Generic;
using System.Linq;
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

    protected static List<NgisFeature> ReadFeatures(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<List<NgisFeature>>(text);
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
        => Read<FeatureCollection>(path);

    protected static CreatePolygonFromLinesRequest ReadCreatePolygonFromLinesRequest(string path) 
        => Read<CreatePolygonFromLinesRequest>(path);

    protected static T Read<T>(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<T>(text);
    }


}