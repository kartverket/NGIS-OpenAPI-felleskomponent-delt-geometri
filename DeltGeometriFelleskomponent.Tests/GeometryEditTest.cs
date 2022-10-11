using System.Collections.Generic;
using System.Linq;
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

    private readonly NgisFeature LineFeatureMoved2 = new()
    {
        Geometry = new LineString(new[]
    {
            new Coordinate(0, 1),
            new Coordinate(2, 3),
            new Coordinate(100, 100)
        })
    };

    private readonly NgisFeature LineFeatureSans1 = new()
    {
        Geometry = new LineString(new[]
        {
            new Coordinate(0, 1),
            new Coordinate(4, 5)
        })
    };

    private readonly NgisFeature LineFeatureNew0 = new()
    {
        Geometry = new LineString(new[]
        {
            new Coordinate(100, 100),
            new Coordinate(0, 1),
            new Coordinate(2, 3),
            new Coordinate(4, 5),            
        })
    };

    private readonly NgisFeature LineFeatureNew1 = new()
    {
        Geometry = new LineString(new[]
    {
            new Coordinate(0, 1),
            new Coordinate(100, 100),
            new Coordinate(2, 3),
            new Coordinate(4, 5),
        })
    };

    private readonly NgisFeature LineFeatureNew2 = new()
    {
        Geometry = new LineString(new[]
{
            new Coordinate(0, 1),            
            new Coordinate(2, 3),
            new Coordinate(100, 100),
            new Coordinate(4, 5),
        })
    };

    private readonly NgisFeature LineFeatureNew3 = new()
    {
        Geometry = new LineString(new[]
{
            new Coordinate(0, 1),
            new Coordinate(2, 3),            
            new Coordinate(4, 5),
            new Coordinate(100, 100),
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




    [Fact]
    public void DeletesNodeOnLineWithWithNewFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureSans1),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Delete
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(2, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureSans1.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
    }


    [Fact]
    public void InsertsNodeOnLineIndex0WithNewFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew0),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Insert,
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(100, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew0.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex1WithNewFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew1),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Insert,
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(100, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[2].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex2WithOldFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew2),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Insert,
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(100, res.Geometry.Coordinates[2].X);

        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex3WithOldFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew3),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Insert,
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[2].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(100, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void MovesNodeOnLineIndex2WithOldFeature()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            AffectedFeatures = new List<NgisFeature>(),
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureMoved2),
            Edit = new EditLineOperation()
            {
                Operation = EditOperation.Edit,
            }
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(3, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureMoved2.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureMoved2.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(100, res.Geometry.Coordinates[2].X);
        
    }


    [Fact]
    public void DeletesNodeOnLineWithWithNewFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureSans1)
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(2, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureSans1.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
    }


    [Fact]
    public void InsertsNodeOnLineIndex0WithNewFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew0)
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(100, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew0.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex1WithNewFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew1)
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(100, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[2].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(LineFeatureNew1.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex2WithOldFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew2),
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(100, res.Geometry.Coordinates[2].X);

        Assert.Equal(LineFeatureNew2.Geometry.Coordinates[3].X, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void InsertsNodeOnLineIndex3WithOldFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureNew3)
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(4, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(LineFeatureNew3.Geometry.Coordinates[2].X, res.Geometry.Coordinates[2].X);
        Assert.Equal(100, res.Geometry.Coordinates[3].X);
    }

    [Fact]
    public void MovesNodeOnLineIndex2WithOldFeatureSansEdit()
    {
        var res = GeometryEdit.EditObject(new EditLineRequest()
        {
            Feature = NgisFeatureHelper.Copy(LineFeature),
            NewFeature = NgisFeatureHelper.Copy(LineFeatureMoved2)
        })[0];
        output.WriteLine("editedFeature: " + res.Geometry);
        Assert.Equal(3, res.Geometry.Coordinates.Length);
        Assert.Equal(LineFeatureMoved2.Geometry.Coordinates[0].X, res.Geometry.Coordinates[0].X);
        Assert.Equal(LineFeatureMoved2.Geometry.Coordinates[1].X, res.Geometry.Coordinates[1].X);
        Assert.Equal(100, res.Geometry.Coordinates[2].X);

    }
}