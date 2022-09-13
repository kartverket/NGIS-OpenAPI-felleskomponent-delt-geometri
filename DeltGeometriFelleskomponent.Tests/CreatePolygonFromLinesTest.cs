using System.Collections.Generic;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
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

        var result = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { cwLine, ccwLine } });

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

        var result = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { cwLine, ccwLine, cwInnerLine, ccwInnerLine } });

        Assert.True(result.IsValid);
        Assert.Equal(5, result.AffectedFeatures.Count);

        var polygon = result.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");
        
        Assert.Single(polygon!.Geometry_Properties!.Interiors!);
        var interior1 = polygon!.Geometry_Properties!.Interiors![0];
        Assert.Equal("3", interior1[0]);
        Assert.Equal("-4", interior1[1]);
    }

}