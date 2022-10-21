using Xunit.Abstractions;
using Xunit;
using DeltGeometriFelleskomponent.TopologyImplementation;
using DeltGeometriFelleskomponent.Models;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Tests;

public class PolygonEditorTest : TestBase
{
    private readonly ITestOutputHelper output;

    public PolygonEditorTest(ITestOutputHelper output)
    {
        // Capturing output in unit tests
        this.output = output;
    }

    [Fact]
    public void EditsPolygonWithOneRingAndNoOtherReferences()
    {
        //arrange
        var line = GetExampleFeature("8");
        var res = new PolygonCreator().CreatePolygonFromLines(new List<NgisFeature>() { line }, null);
        var polygon = res.First().AffectedFeatures.First(f => f.Geometry.GeometryType == "Polygon");

        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates;
        ring[1].X = ring[1].X + 0.00001;
        var geometry = new Polygon(new LinearRing(ring));

        //act
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest() { 
            Feature = polygon, 
            AffectedFeatures= new List<NgisFeature> { line },
            EditedGeometry = geometry
        });


    }

}
