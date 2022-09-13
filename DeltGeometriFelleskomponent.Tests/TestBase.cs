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
        var text = System.IO.File.ReadAllText("Examples/example_geometries.geojson");
        var features = GeoJsonConvert.DeserializeObject<FeatureCollection>(text);
        var feature = features.FirstOrDefault(f => (string)f.Attributes["id"] == id);
        if (feature != null)
        {
            return NgisFeatureHelper.CreateFeature(feature.Geometry, id);
        }
        throw new Exception($"Feature with id={id} not found");
    }

}