using System.Collections.Generic;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit;
using Xunit.Abstractions;

namespace DeltGeometriFelleskomponent.Tests;

public class GeometryEditTest : TestBase
{
    private readonly NgisFeature LineFeature = new()
    {
        Geometry = new LineString(new[]
        {
            new Coordinate(0, 1),
            new Coordinate(2, 3),
            new Coordinate(4, 5)
        })
    };

    private readonly ITestOutputHelper output;

    public GeometryEditTest(ITestOutputHelper output)
    {
        // Capturing output in unit tests
        this.output = output;
    }

    [Fact]
    public void DeletesNodeOnLine()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            Edit = new EditLineOperation()
            {
                NodeIndex = 1,
                Operation = EditOperation.Delete
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(2, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeature.Geometry.Coordinates[2].X, res.Geometry.Coordinates[1].X);
    }


    [Fact]
    public void InsertsNodeOnLineIndex0()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            Edit = new EditLineOperation()
            {
                NodeIndex = 0,
                Operation = EditOperation.Insert,
                NodeValue = new List<double>() { 100, 100 }
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(100, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[2].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex1()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            Edit = new EditLineOperation()
            {
                NodeIndex = 1,
                Operation = EditOperation.Insert,
                NodeValue = new List<double>(){100, 100}
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeature.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(100, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[1].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[2].X, res.Geometry.Coordinates[3].X);
    }
    [Fact]
    public void InsertsNodeOnLineIndex2()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            Edit = new EditLineOperation()
            {
                NodeIndex = 2,
                Operation = EditOperation.Insert,
                NodeValue = new List<double>() { 100, 100 }
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeature.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(100, res.Geometry.Coordinates[2].X);
        
        Assert.Equal(LineFeature.Geometry.Coordinates[2].X, res.Geometry.Coordinates[3].X);
    }
    [Fact]
    public void InsertsNodeOnLineIndex3()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            Edit = new EditLineOperation()
            {
                NodeIndex = 3,
                Operation = EditOperation.Insert,
                NodeValue = new List<double>() { 100, 100 }
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeature.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeature.Geometry.Coordinates[2].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(100, res.Geometry.Coordinates[3].X);
    }
}