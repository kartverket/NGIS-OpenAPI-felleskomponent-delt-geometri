using System;
using System.Collections.Generic;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Xunit;

namespace DeltGeometriFelleskomponent.Tests
{
    public class TopologyImplementationTest
    {
        private readonly ITopologyImplementation _topologyImplementation =
            new TopologyImplementation.TopologyImplementation();

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
                    Type = "Kaiomr�de",
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
                Type = "Kaiomr�deGrense"
            };

            var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            {
                Feature = new NgisFeature()
                {
                    Geometry = new Polygon(null),
                    Type = "Kaiomr�de",
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
    }
}