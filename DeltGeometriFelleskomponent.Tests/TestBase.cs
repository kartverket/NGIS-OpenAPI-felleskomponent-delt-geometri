using System.Text.Json;
using System.Text.Json.Serialization;
using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.Tests;

public abstract class TestBase
{
    protected NgisFeature ReadFeature(string path)
    {
        var text = System.IO.File.ReadAllText(@"Examples\polygon_replace.geojson");
        var feature = JsonSerializer.Deserialize<NgisFeature>(text, new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true,
            Converters = { 
                new JsonStringEnumConverter(),
                new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()
            }
        });

        return feature;

    }

}