using System.Collections.Generic;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Xunit;

namespace DeltGeometriFelleskomponent.Tests;

public class CreatePolygonFromLinesTest: TestBase
{

    private readonly ITopologyImplementation _topologyImplementation =
        new TopologyImplementation.TopologyImplementation();

    [Fact]
    public void OrdersExteriorReferencesTheRightWay()
    {
        var cwLine = GetExampleFeature("1");
        var ccwLine = GetExampleFeature("2");

        var result = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { cwLine, ccwLine } }).First();

        Assert.True(result.IsValid);
        Assert.Equal(3, result.AffectedFeatures.Count);

        var polygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        var exteriors = polygon!.Geometry_Properties!.Exterior!;
        Assert.Equal(2, exteriors.Count);
        
        Assert.Equal("-1", exteriors[0]);
        Assert.Equal("2", exteriors[1]);

        Assert.Empty( polygon!.Geometry_Properties!.Interiors!);
    }

    [Fact]
    public void OrdersInteriorReferencesTheRightWay()
    {
        var cwLine = GetExampleFeature("1");
        var ccwLine = GetExampleFeature("2");

        var cwInnerLine = GetExampleFeature("3");
        var ccwInnerLine = GetExampleFeature("4");

        var result = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { cwLine, ccwLine, cwInnerLine, ccwInnerLine } }).First();

        Assert.True(result.IsValid);
        Assert.Equal(5, result.AffectedFeatures.Count);

        var polygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        
        Assert.Single(polygon!.Geometry_Properties!.Interiors!);
        var interior1 = polygon!.Geometry_Properties!.Interiors![0];
        Assert.Equal("3", interior1[0]);
        Assert.Equal("-4", interior1[1]);
    }

    [Fact]
    public void CreatesMultiplePolygonsWithSharedLine()
    {
        var request = CreateTwoTrianglesWithSharedBorder();

        var result = _topologyImplementation.CreatePolygonsFromLines(request);

        result.ToList().ForEach(r =>
        {
            var polygon = r.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            Assert.NotNull(polygon);

            Assert.True(r.IsValid);

            Assert.Equal(3, r.AffectedFeatures.Count);
        });
    }

    [Fact]
    public void CreatesMultiplePolygonsWithSharedLineAndHole()
    {
        var request = CreateTwoTrianglesWithSharedBorderAndHoleInLeftTriangle();

        var result = _topologyImplementation.CreatePolygonsFromLines(request);

        result.ToList().ForEach(r =>
        {
            Assert.True(r.IsValid);

            var polygon = r.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            Assert.NotNull(polygon);
        });
    }

    private static CreatePolygonFromLinesRequest CreateTwoTrianglesWithSharedBorderAndHoleInLeftTriangle()
    {
        var request = CreateTwoTrianglesWithSharedBorder();

        request.Features.Add(NgisFeatureHelper.CreateFeature(new LinearRing(new List<Coordinate> {
            new Coordinate{X = -0.1, Y= .25},
            new Coordinate{X = -0.5, Y= .5},
            new Coordinate{X = -0.1, Y= .75},
            new Coordinate{X = -0.1, Y= .25}
        }.ToArray())));
        return request;
    }

    private static CreatePolygonFromLinesRequest CreateTwoTrianglesWithSharedBorder()
    {
        var startPoint = new Coordinate { X = 0, Y = 0 };

        var endPoint = new Coordinate { X = 0, Y = 1 };

        var leftPoints = new List<Coordinate> {
            startPoint,
            new Coordinate { X=-.5, Y = .5 },
           endPoint
        }.ToArray();

        var middlePoints = new List<Coordinate> {
            startPoint,
            endPoint
        }.ToArray();

        var rightPoints = new List<Coordinate> {
            startPoint,
            new Coordinate { X=.5, Y = .5 },
            endPoint
        }.ToArray();


        var leftLineString = NgisFeatureHelper.CreateFeature(new LineString(leftPoints));

        var middleLineString = NgisFeatureHelper.CreateFeature(new LineString(middlePoints));

        var rightLineString = NgisFeatureHelper.CreateFeature(new LineString(rightPoints));

        return new CreatePolygonFromLinesRequest()
        { Features = new List<NgisFeature>() { leftLineString, middleLineString, rightLineString } };
    }
}