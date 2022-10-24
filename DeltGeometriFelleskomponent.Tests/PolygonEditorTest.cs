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
    public void EditsPolygonWithOneRingAndNoOtherReferencesByEditingPoint()
    {
        //arrange        
        //build a polygon from a line
        var line = GetExampleFeature("8");
        var res = new PolygonCreator().CreatePolygonFromLines(new List<NgisFeature>() { line }, null);
        var polygon = res.First().AffectedFeatures.First(f => f.Geometry.GeometryType == "Polygon");

        output.WriteLine($"original: {polygon.Geometry}");

        //move one of the vertices of the line and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates;
        ring[1].X = ring[1].X + 0.00001;
        var geometry = new Polygon(new LinearRing(ring));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest() { 
            Feature = NgisFeatureHelper.Copy(polygon), 
            AffectedFeatures = new List<NgisFeature> { line },
            EditedGeometry = geometry
        });

        //assert

        var editedPolygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates[1].X + 0.00001, ((Polygon)editedPolygon.Geometry).Shell.Coordinates[1].X);
        output.WriteLine($"edited:   {editedPolygon.Geometry}");

    }

    [Fact]
    public void EditsPolygonWithOneRingAndNoOtherReferencesByRemovingPoint()
    {
        //arrange        
        //build a polygon from a line
        var line = GetExampleFeature("8");
        var res = new PolygonCreator().CreatePolygonFromLines(new List<NgisFeature>() { line }, null);
        var polygon = res.First().AffectedFeatures.First(f => f.Geometry.GeometryType == "Polygon");

        output.WriteLine($"original: {polygon.Geometry}");

        //delete vertex at index 1  and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList();
        ring.RemoveAt(1);
        var geometry = new Polygon(new LinearRing(ring.ToArray()));

        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = new List<NgisFeature> { line },
            EditedGeometry = geometry
        });

        //assert

        var editedPolygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length -1, ((Polygon)editedPolygon.Geometry).Shell.Coordinates.Length);
        output.WriteLine($"edited:   {editedPolygon.Geometry}");
    }

    [Fact]
    public void EditsPolygonWithOneRingAndNoOtherReferencesByAddingPoint()
    {
        //arrange        
        //build a polygon from a line
        var line = GetExampleFeature("8");
        var res = new PolygonCreator().CreatePolygonFromLines(new List<NgisFeature>() { line }, null);
        var polygon = res.First().AffectedFeatures.First(f => f.Geometry.GeometryType == "Polygon");

        output.WriteLine($"original: {polygon.Geometry}");

        //insert a vertex at index = 1 and create a new polygon
        var ring = ((Polygon)polygon.Geometry).Shell.Copy().Coordinates.ToList();
        ring.Insert(1, new Coordinate(ring[1].X + 0.00001, ring[1].Y + 0.00001));
        var geometry = new Polygon(new LinearRing(ring.ToArray()));
        //act
        //ie: apply this change
        var result = PolygonEditor.EditPolygon(new EditPolygonRequest()
        {
            Feature = NgisFeatureHelper.Copy(polygon),
            AffectedFeatures = new List<NgisFeature> { line },
            EditedGeometry = geometry
        });

        //assert

        var editedPolygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        Assert.Equal(((Polygon)polygon.Geometry).Shell.Coordinates.Length +1, ((Polygon)editedPolygon.Geometry).Shell.Coordinates.Length);
        output.WriteLine($"edited:   {editedPolygon.Geometry}");
    }

}
