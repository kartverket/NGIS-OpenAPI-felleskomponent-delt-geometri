using System.Collections.Generic;
using DeltGeometriFelleskomponent.Models;
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

    [Fact]
    public void WritesBounds()
    {
        var featuretype = "Featuretype1";
        var bounds = new List<BoundsReference>()
        {
            new ()
            {
                Featuretype =featuretype,
                LokalId = "id_2",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            },
            new ()
            {
                Featuretype = featuretype,
                LokalId = "id_3",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            }
        };

        const string id = "id_1";
        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        NgisFeatureHelper.SetBoundsReferences(feature,bounds, featuretype);
        Assert.NotNull(feature.Properties["avgrensesAvFeaturetype1"]);

        
    }

    [Fact]
    public void ReadsBounds()
    {
        var featuretype1 = "Featuretype1";
        var bounds1 = new List<BoundsReference>()
        {
            new ()
            {
                Featuretype =featuretype1,
                LokalId = "id_2",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            },
            new ()
            {
                Featuretype = featuretype1,
                LokalId = "id_3",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            }
        };

        var featuretype2 = "Featuretype2";
        var bounds2 = new List<BoundsReference>()
        {
            new ()
            {
                Featuretype =featuretype2,
                LokalId = "id_4",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            },
            new ()
            {
                Featuretype = featuretype2,
                LokalId = "id_4",
                Idx = new List<int>(){0,0,0},
                Reverse = false
            }
        };

        const string id = "id_1";
        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        NgisFeatureHelper.SetBoundsReferences(feature, bounds1, featuretype1);
        NgisFeatureHelper.SetBoundsReferences(feature, bounds2, featuretype2);

        var bounds = NgisFeatureHelper.GetBoundsReferences(feature);
        Assert.Equal(bounds1.Count + bounds2.Count, bounds.Count);
        
    }
}