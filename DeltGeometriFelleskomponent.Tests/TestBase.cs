using System.Text.Json;
using System.Text.Json.Serialization;
using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.Tests;

public abstract class TestBase
{
    protected static NgisFeature ReadFeature(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<NgisFeature>(text, new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true,
            Converters = { 
                new JsonStringEnumConverter(),
                new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()
            }
        })!;
    }

}