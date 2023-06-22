using System.Collections.Generic;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit.Abstractions;
using Xunit;
using System;
using System.IO;

namespace DeltGeometriFelleskomponent.Tests;

public class CreatePolygonFromLinesTest: TestBase
{

    protected readonly ITestOutputHelper output;

    public CreatePolygonFromLinesTest(ITestOutputHelper output)
    {
        // Capturing output in unit tests
        this.output = output;
    }

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



        var exteriors = ReferenceHelper.GetExteriorReferences(polygon!);
        Assert.Equal(2, exteriors.Count);

        Assert.False(exteriors[0].Reverse);
        Assert.True(exteriors[1].Reverse);
        
        var interiors = ReferenceHelper.GetInteriorReferences(polygon!);
        Assert.Empty(interiors);
    }

    
    [Theory]
    [InlineData("Correct order",new string[] { "8a454969-03f4-48f8-b445-274cce15b62f", "5b22a1de-ed10-4809-81da-c9bd2fea2bb8", "239848d3-9a03-418b-898f-69efa45e4c11", "cfe0e792-5320-40a1-aa00-9def7841663e", "8d005937-384b-4fc9-8aae-9a8e6a65c611" })]
    [InlineData("reversed", new string[] {"8d005937-384b-4fc9-8aae-9a8e6a65c611","cfe0e792-5320-40a1-aa00-9def7841663e","239848d3-9a03-418b-898f-69efa45e4c11","5b22a1de-ed10-4809-81da-c9bd2fea2bb8","8a454969-03f4-48f8-b445-274cce15b62f"  })]
    [InlineData("random", new string[] { "cfe0e792-5320-40a1-aa00-9def7841663e", "8d005937-384b-4fc9-8aae-9a8e6a65c611", "5b22a1de-ed10-4809-81da-c9bd2fea2bb8", "239848d3-9a03-418b-898f-69efa45e4c11", "8a454969-03f4-48f8-b445-274cce15b62f" })]
    public void AddsExteriorsInCorrectOrder(string a, string[] ids) {      
        var geometries = ReadCreatePolygonFromLinesRequest("Examples/complex_geometries_example.geojson").Features;
        
        var lines = ids.Select(id => geometries.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == id)).OfType<NgisFeature>().ToList();

        var result = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest() { Features = lines }).First();
        Assert.True(result.IsValid);
        var polygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        output.WriteLine(polygon.Geometry.ToString());
        if (polygon.Geometry_Properties?.Exterior != null)
        {
            foreach (var exterior in polygon.Geometry_Properties?.Exterior)
            {
                output.WriteLine(exterior);
            }

            Assert.NotNull(polygon);

            var order = new string[]
            {
                "-8d005937-384b-4fc9-8aae-9a8e6a65c611", "-cfe0e792-5320-40a1-aa00-9def7841663e",
                "-239848d3-9a03-418b-898f-69efa45e4c11", "-5b22a1de-ed10-4809-81da-c9bd2fea2bb8",
                "-8a454969-03f4-48f8-b445-274cce15b62f"
            };

            var ordered = StartAt(polygon.Geometry_Properties.Exterior.ToArray(),
                polygon.Geometry_Properties.Exterior.IndexOf(order[0]));

            Assert.Equal(order[0], ordered[0]);
            Assert.Equal(order[1], ordered[1]);
            Assert.Equal(order[2], ordered[2]);
            Assert.Equal(order[3], ordered[3]);
            Assert.Equal(order[4], ordered[4]);
        }
    }

    private static List<string> StartAt(string[] lst, int idx)
    {        
        var a = lst[idx..^0];
        var b = lst[0..idx];
        return a.Concat(b).ToList();
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

        var interiors = ReferenceHelper.GetInteriorReferences(polygon!);

        Assert.Single(interiors);
        var interior1 = interiors[0];
        Assert.True(interior1[0].Reverse);
        Assert.False(interior1[1].Reverse);        
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

        Assert.True(result.Count() == 2);

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

        request.Centroids = new List<Point> { 
            new Point (-.05, .5 ),
            new Point (.25, .5 ) };

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