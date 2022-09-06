using System.Text.Json;
using System.Text.Json.Serialization;
using DeltGeometriFelleskomponent.Api.Util;
using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.Tests;

public abstract class TestBase
{
    protected static NgisFeature ReadFeature(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        return GeoJsonConvert.DeserializeObject<NgisFeature>(text)!;
    }

}