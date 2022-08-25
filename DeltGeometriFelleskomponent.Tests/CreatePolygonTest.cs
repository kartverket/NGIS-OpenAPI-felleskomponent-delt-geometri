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
}