using System.Collections.Generic;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit;

namespace DeltGeometriFelleskomponent.Tests;

public class NgisFeatureHelperTest: TestBase
{
    [Fact]
    public void ReadsLokalId()
    {
        var feature = ReadFeature(@"Examples\polygon_replace.geojson");
        Assert.Equal("6fb23b81-b757-4a04-9f82-65639d88656b", NgisFeatureHelper.GetLokalId(feature));
    }

    [Fact]
    public void WritesLokalId()
    {
        const string id = "id_1";

        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature));
    }

    
}