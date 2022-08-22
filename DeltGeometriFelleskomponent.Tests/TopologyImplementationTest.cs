using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Xunit;
using Xunit.Abstractions;

namespace DeltGeometriFelleskomponent.Tests
{
    public class TopologyImplementationTest
    {
        private readonly ITopologyImplementation _topologyImplementation =
            new TopologyImplementation.TopologyImplementation();

        private readonly ITestOutputHelper output;

        public TopologyImplementationTest(ITestOutputHelper output)
        {
            // Capturing output in unit tests
            this.output = output;
        }

        [Fact]
        public void ReturnsPointWhenCreatingPoint()
        {
            var point = new Point(1, 2);
            var id = Guid.NewGuid().ToString();

            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = new NgisFeature() { Geometry = point, LocalId = id, Operation = Operation.Create },
                
            });

            Assert.Single(res.AffectedFeatures);
            var feature = res.AffectedFeatures.First();
            Assert.Equal(point,feature.Geometry);
            Assert.Equal(id, feature.LocalId);
            Assert.Equal(Operation.Create, feature.Operation);
        }

        [Fact]
        public void ReturnsLineAndPolygonWhenCreatingPolygonFromPolygon()
        {
            var linearRing = new LinearRing(new[]
            {
                new Coordinate(0, 0), 
                new Coordinate(0, 1), 
                new Coordinate(1, 1),
                new Coordinate(1, 0),
                new Coordinate(0, 0),
            });
            var polygon = new Polygon(linearRing);
            var id = Guid.NewGuid().ToString();
            
            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = new NgisFeature() { 
                    Geometry = polygon, 
                    LocalId = id, 
                    Type = "Kaiområde",
                    Operation = Operation.Create 
                },

            });

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("Polygon", feature1.Geometry!.GeometryType);
            Assert.Equal(id, feature1.LocalId);
            Assert.Equal(Operation.Create, feature1.Operation);

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, feature1.Operation);

            Assert.Single(feature1.References!);
            Assert.Equal(feature1.References!.First(), feature2.LocalId);
        }


        [Fact]
        public void ReturnsLineAndPolygonWhenCreatingPolygonFromLine()
        {

            var id = Guid.NewGuid().ToString();

            var linearRing = new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(0, 1),
                new Coordinate(1, 1),
                new Coordinate(1, 0),
                new Coordinate(0, 0),
            });

            var linestring = new LineString(linearRing.Coordinates);

            var lineFeature = new NgisFeature()
            {
                Geometry = linestring,
                LocalId = id,
                Operation = Operation.Create,
                Type = "KaiområdeGrense"
            };

            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = new NgisFeature()
                {
                    Geometry = new Polygon(null),
                    Type = "Kaiområde",
                    Operation = Operation.Create,
                    References = new List<string>() { id}
                },
                AffectedFeatures = new List<NgisFeature>() { lineFeature}
            });

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, feature1.LocalId);
            Assert.Equal(Operation.Create, feature1.Operation);

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("Polygon", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, feature1.Operation);

            Assert.Single(feature1.References!);
            Assert.Equal(feature1.References!.First(), feature2.LocalId);
        }

        [Fact]
        public void ReturnsLinesAndPolygonWhenCreatingPolygonFrom2Lines()
        {
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple ordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered:true);
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered:false);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings and check for Point Inside Area");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: false, insideCheck:true);
        }
        
        private void GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered=true, bool insideCheck = false)
        // private void GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered=true, double? x=null, double? y=null)
        {
            var id = Guid.NewGuid().ToString();

            var linestring = new LineString(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100)
            });


            var lineFeature = new NgisFeature()
            {
                Geometry = linestring,
                LocalId = id,
                Operation = Operation.Create,
                Type = "KaiområdeGrense"
            };


            var id2 = Guid.NewGuid().ToString();

            LineString linestring2;
            if (ordered)
            {
                linestring2 = new LineString(new[]
                {
                    new Coordinate(100, 100),
                    new Coordinate(0, 100),
                    new Coordinate(0, 0)
                });
            }
            else
            {
                // Unordered direction (could use linestring2.reverse)
                linestring2 = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(0, 100),
                    new Coordinate(100, 100)
                });

            }


            var lineFeature2 = new NgisFeature()
            {
                Geometry = linestring2,
                LocalId = id2,
                Operation = Operation.Create,
                Type = "KaiområdeGrense"
            };

            Point? centroid = null;
            if (insideCheck)
            {
                // Check if polygon  entirely contains the given coordinate location
                centroid = new Point(new Coordinate(50, 50));

            }

        
            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = new NgisFeature()
                {
                    Geometry = new Polygon(null),
                    Type = "Kaiområde",
                    Operation = Operation.Create,
                    References = new List<string>() { id, id2 },
                    Centroid = centroid
                },
                AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2 }
            });

            Assert.Equal(3, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, feature1.LocalId);
            Assert.Equal(Operation.Create, feature1.Operation);

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, feature1.Operation);

            var feature3 = res.AffectedFeatures.ElementAt(2); // polygon

            Assert.Equal("Polygon", feature3.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, feature1.Operation);


            Assert.Single(feature1.References!);
            Assert.Equal(feature1.References!.First(), feature3.LocalId);

            Assert.Single(feature2.References!);
            Assert.Equal(feature2.References!.First(), feature3.LocalId);

            Assert.Equal(2, feature3.References!.Count);
            Assert.Equal(feature3.References!.First(), feature1.LocalId);
            Assert.Equal(feature3.References!.Last(), feature2.LocalId);

            if (insideCheck)
            {
                output.WriteLine("InsideCheck for Point Inside Area:{0}", res.IsValid);
                // Assert.True(res.IsValid);
            }
        }
      
    }
}