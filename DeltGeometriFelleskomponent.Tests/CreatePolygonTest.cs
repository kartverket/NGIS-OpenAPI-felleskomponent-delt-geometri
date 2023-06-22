using System.Linq;
using System.Text.Json;
using DeltGeometriFelleskomponent.Api.Util;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
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

        var result = _topologyImplementation.CreateGeometry(new CreateGeometryRequest() { Feature = feature });

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

        var exteriors = ReferenceHelper.GetExteriorReferences(polygon);
        //make sure the polygon references the line
        Assert.Single(exteriors);
        Assert.Empty(ReferenceHelper.GetInteriorReferences(polygon));
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring), exteriors.First().LokalId);

        //make sure that the linestring doesn't have any references
        Assert.Null(linestring.Geometry_Properties);
    }

    [Fact]
    public void CreatesTwoLinestringAndPolygonFromProvidedPolygonWithHole()
    {
        var feature = ReadFeature(@"Examples\polygon_with_hole_create.geojson");

        var result = _topologyImplementation.CreateGeometry(new CreateGeometryRequest() { Feature = feature });

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
        var exterior = ReferenceHelper.GetExteriorReferences(polygon);
        Assert.Single(exterior);
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring1), exterior.First().LokalId);

        var interiors = ReferenceHelper.GetInteriorReferences(polygon);
        Assert.Single(interiors);
        Assert.Single(interiors.First());
        Assert.Equal(NgisFeatureHelper.GetLokalId(linestring2), interiors.First().First().LokalId);

        //make sure that the linestrings doesn't have any references
        Assert.Null(linestring1.Geometry_Properties);
        Assert.Null(linestring2.Geometry_Properties);
    }

    [Fact]
    public void AddsReferencesInCorrectOrder()
    {
        var request = ReadCreatePolygonFromLinesRequest("Examples/polygon_from_lines_input.json");

        var result = _topologyImplementation.CreatePolygonsFromLines(request);

        var polygon = result.FirstOrDefault().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");


        Assert.NotNull(polygon);

        var exteriors = ReferenceHelper.GetExteriorReferences(polygon!);

        Assert.Equal("e8669bf0-430b-4a3b-b903-eed024453116", exteriors[0].LokalId);
        Assert.False(exteriors[0].Reverse);
        Assert.Equal("91bf85e6-1909-4c36-937d-a95a046189ef", exteriors[1].LokalId);
        Assert.True(exteriors[1].Reverse);
        Assert.Equal("90bad013-f611-46f5-bb59-d60eb9552d0e", exteriors[2].LokalId);
        Assert.True(exteriors[2].Reverse);
    }


    [Fact]
    public void PolygonSerializationHandlesRingWinding()
    {
        var poly = new Polygon(new LinearRing(new[]
        {
            new Coordinate(1, 1),
            new Coordinate(1, 2),
            new Coordinate(2, 2),
            new Coordinate(2, 1),
            new Coordinate(1, 1),
        }));
        
        var geojson2 = GeoJsonConvert.SerializeObject(PolygonCreator.EnsureOrdering(poly));

        Assert.Equal("{\"type\":\"Polygon\",\"coordinates\":[[[1.0,1.0],[2.0,1.0],[2.0,2.0],[1.0,2.0],[1.0,1.0]]]}", geojson2);
    }
}