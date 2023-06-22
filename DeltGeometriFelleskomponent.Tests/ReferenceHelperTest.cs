using DeltGeometriFelleskomponent.Models;
using System.Collections.Generic;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit;
using static DeltGeometriFelleskomponent.TopologyImplementation.PolygonCreator;
using System.Linq;

namespace DeltGeometriFelleskomponent.Tests;

public class ReferenceHelperTest: TestBase
{
    [Fact]
    public void WritesBounds()
    {
        const string id = "id_1";
        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        ReferenceHelper.AddBoundsReferences(feature, Bounds1, Featuretype1);
        Assert.NotNull(feature.Properties["avgrensesAvFeaturetype1"]);
    }

    [Fact]
    public void ReadsBounds()
    {
        const string id = "id_1";
        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        ReferenceHelper.AddBoundsReferences(feature, Bounds1, Featuretype1);
        ReferenceHelper.AddBoundsReferences(feature, Bounds2, Featuretype2);

        var bounds = ReferenceHelper.GetBoundsReferences(feature);
        Assert.Equal(Bounds1.Count + Bounds2.Count, bounds.Count);
    }

    [Fact]
    public void SetsReferencesWithOneNonReversedExterior()
    {
        var feature = GetFeature("id_1", Featuretype1);

        var boundsFeature1 = GetFeature("id_2", Featuretype2);

        var ref1 = new FeatureWithDirection()
        {
            IsReversed = false,
            Feature = boundsFeature1
        };

        ReferenceHelper.SetReferences(feature, new List<FeatureWithDirection>() { ref1 }, null);
        
        var bounds = ReferenceHelper.GetBoundsReferences(feature);
        Assert.Single(bounds);
        Assert.False(bounds[0].Reverse);
        Assert.Equal(Featuretype2, bounds[0].Featuretype);
        Assert.Equal(0, bounds[0].Idx[0]);
        Assert.Equal(0, bounds[0].Idx[1]);
        Assert.Equal(0, bounds[0].Idx[2]);

        Assert.True(feature.Properties.Exists($"avgrensesAv{Featuretype2}"));
    }

    [Fact]
    public void SetsReferencesWithTwoFeatureTypesAsExterior()
    {
        var feature = GetFeature("id_1", Featuretype1);

        var boundsFeature1 = GetFeature("id_2", Featuretype2);
        var boundsFeature2 = GetFeature("id_3", Featuretype3);

        var ref1 = new FeatureWithDirection()
        {
            IsReversed = false,
            Feature = boundsFeature1
        };
        var ref2 = new FeatureWithDirection()
        {
            IsReversed = true,
            Feature = boundsFeature2
        };

        ReferenceHelper.SetReferences(feature, new List<FeatureWithDirection>() { ref1, ref2 }, null);

        var bounds = ReferenceHelper.GetBoundsReferences(feature);
        Assert.Equal(2, bounds.Count);
        Assert.False(bounds[0].Reverse);
        Assert.Equal(Featuretype2, bounds[0].Featuretype);
        Assert.Equal(0, bounds[0].Idx[0]);
        Assert.Equal(0, bounds[0].Idx[1]);
        Assert.Equal(0, bounds[0].Idx[2]);

        Assert.True(bounds[1].Reverse);
        Assert.Equal(Featuretype3, bounds[1].Featuretype);
        Assert.Equal(0, bounds[1].Idx[0]);
        Assert.Equal(0, bounds[1].Idx[1]);
        Assert.Equal(1, bounds[1].Idx[2]);

        Assert.True(feature.Properties.Exists($"avgrensesAv{Featuretype2}"));
        Assert.True(feature.Properties.Exists($"avgrensesAv{Featuretype3}"));
    }

    [Fact]
    public void SetsReferencesWithOneInterior()
    {
        var feature = GetFeature("id_1", Featuretype1);

        var boundsFeature1 = GetFeature("id_2", Featuretype2);
        var boundsFeature2 = GetFeature("id_3", Featuretype3);

        var ref1 = new FeatureWithDirection()
        {
            IsReversed = false,
            Feature = boundsFeature1
        };
        var ref2 = new FeatureWithDirection()
        {
            IsReversed = true,
            Feature = boundsFeature2
        };

        ReferenceHelper.SetReferences(feature, new List<FeatureWithDirection>() { ref1 }, new List<IEnumerable<FeatureWithDirection>>(){new List<FeatureWithDirection>(){ ref2 } });

        var bounds = ReferenceHelper.GetBoundsReferences(feature);
        Assert.Equal(2, bounds.Count);
        Assert.False(bounds[0].Reverse);
        Assert.Equal(Featuretype2, bounds[0].Featuretype);
        Assert.Equal(0, bounds[0].Idx[0]);
        Assert.Equal(0, bounds[0].Idx[1]);
        Assert.Equal(0, bounds[0].Idx[2]);

        Assert.True(bounds[1].Reverse);
        Assert.Equal(Featuretype3, bounds[1].Featuretype);
        Assert.Equal(0, bounds[1].Idx[0]);
        Assert.Equal(1, bounds[1].Idx[1]);
        Assert.Equal(0, bounds[1].Idx[2]);

        Assert.True(feature.Properties.Exists($"avgrensesAv{Featuretype2}"));
        Assert.True(feature.Properties.Exists($"avgrensesAv{Featuretype3}"));
    }

    [Fact]
    public void ReadsAvgrensesAvFromGeoJSON()
    {
        var features = ReadFeatures("Examples/example_ngisfeatures_edit.geojson");
        var feature = features.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "f946043d-2c4b-4278-b1ac-6eb8073daac1")!;

        var references = ReferenceHelper.GetBoundsReferences(feature);
        Assert.Equal(2, references.Count);
        Assert.Equal("02999bcc-fe82-4ce6-8a2e-6f01aeac0b8a", references.First().LokalId);
    }

    private static NgisFeature GetFeature(string id, string featureType)
    {
        var feature = NgisFeatureHelper.CreateFeature(new Point(1, 1), id);
        feature.Properties.Add("featuretype", featureType);
        return feature;
    }

    private const string Featuretype1 = "Featuretype1";

    private static readonly List<BoundsReference> Bounds1 = new()
    {
        new BoundsReference
        {
            Featuretype =Featuretype1,
            LokalId = "id_2",
            Idx = new List<int>(){0,0,0},
            Reverse = false
        },
        new BoundsReference
        {
            Featuretype = Featuretype1,
            LokalId = "id_3",
            Idx = new List<int>(){0,0,0},
            Reverse = false
        }
    };

    private const string Featuretype2 = "Featuretype2";
    private const string Featuretype3 = "Featuretype3";

    private static readonly List<BoundsReference> Bounds2 = new()
    {
        new BoundsReference
        {
            Featuretype =Featuretype2,
            LokalId = "id_4",
            Idx = new List<int>(){0,0,0},
            Reverse = false
        },
        new BoundsReference
        {
            Featuretype = Featuretype2,
            LokalId = "id_4",
            Idx = new List<int>(){0,0,0},
            Reverse = false
        }
    };

}