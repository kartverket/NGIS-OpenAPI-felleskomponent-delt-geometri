using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit;

namespace DeltGeometriFelleskomponent.Tests;

public class CreatePolygonTest: TestBase
{
    private readonly ITopologyImplementation _topologyImplementation =
        new TopologyImplementation.TopologyImplementation();

    [Fact]
    public void CreatesLinestringAndPolygonFromProvidedPolygon()
    {
        var feature = ReadFeature(@"Examples\polygon_create.geojson");

        var result = _topologyImplementation.ResolveReferences(new ToplogyRequest() { Feature = feature });

        //we should get back the polygon and a linestring
        Assert.Equal(2, result.AffectedFeatures.Count);

        var polygon = result.AffectedFeatures.First();
        var linestring = result.AffectedFeatures.Last();

        //make sure that the line geometry equals the geometry of the polygon exterior
        Assert.True(linestring.Geometry.Equals(((Polygon)polygon.Geometry).ExteriorRing));

        //make sure localIds are set
        Assert.NotNull(NgisFeatureHelper.GetLokalId(polygon));
        Assert.NotNull(NgisFeatureHelper.GetLokalId(linestring));

        //make sure create is set
        Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(polygon));
        Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(linestring));

        //make sure properties are kept
        Assert.Equal("test", polygon.Properties.GetOptionalValue("test"));

        //make sure the polygon references the line
        Assert.Single(polygon.Geometry_Properties!.Exterior);
        Assert.Null(polygon.Geometry_Properties!.Interiors);
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring), polygon.Geometry_Properties!.Exterior.First());

        //make sure that the linestring doesn't have any references
        Assert.Null(linestring.Geometry_Properties);
    }

    [Fact]
    public void CreatesTwoLinestringAndPolygonFromProvidedPolygonWithHole()
    {
        var feature = ReadFeature(@"Examples\polygon_with_hole_create.geojson");

        var result = _topologyImplementation.ResolveReferences(new ToplogyRequest() { Feature = feature });

        //we should get back the polygon and two linestrings (one exterior, one interior)
        Assert.Equal(3, result.AffectedFeatures.Count);

        var polygon = result.AffectedFeatures.First();
        var linestring1 = result.AffectedFeatures[1];
        var linestring2 = result.AffectedFeatures[2];

        //make sure that the first line geometry equals the geometry of the polygon exterior
        Assert.True(linestring1.Geometry.Equals(((Polygon)polygon.Geometry).ExteriorRing));

        //make sure that the second line geometry equals the geometry of the polygon interior
        Assert.True(linestring2.Geometry.Equals(((Polygon)polygon.Geometry).InteriorRings[0]));
        
        //make sure localIds are set
        Assert.NotNull(NgisFeatureHelper.GetLokalId(polygon));
        Assert.NotNull(NgisFeatureHelper.GetLokalId(linestring1));
        Assert.NotNull(NgisFeatureHelper.GetLokalId(linestring2));

        //make sure create is set
        Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(polygon));
        Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(linestring1));
        Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(linestring2));

        //make sure properties are kept
        Assert.Equal("test", polygon.Properties.GetOptionalValue("test"));

        //make sure the polygon references the lines
        Assert.Single(polygon.Geometry_Properties!.Exterior);
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring1), polygon.Geometry_Properties!.Exterior.First());

        Assert.Single(polygon.Geometry_Properties!.Interiors!);
        Assert.Single(polygon.Geometry_Properties!.Interiors!.First());
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring2), polygon.Geometry_Properties!.Interiors!.First().First());

        //make sure that the linestrings doesn't have any references
        Assert.Null(linestring1.Geometry_Properties);
        Assert.Null(linestring2.Geometry_Properties);
    }
}