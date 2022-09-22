using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
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
                Feature = NgisFeatureHelper.CreateFeature(point, id, Operation.Create)

            });

            Assert.Single(res.AffectedFeatures);
            var feature = res.AffectedFeatures.First();
            Assert.Equal(point, feature.Geometry);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature));
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
                //Type = "Kaiomr�de",
                Feature = NgisFeatureHelper.CreateFeature(polygon, id, Operation.Create)
            });

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("Polygon", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));

            var references = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(references);
            Assert.Equal(references.First(), NgisFeatureHelper.GetLokalId(feature2));
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

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);

            //Type = "Kaiomr�de",

            // _topologyImplementation.CreatePolygonFromLines replaces _topologyImplementation.ResolveReferences
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = new List<NgisFeature>() { lineFeature },
                Centroid = null
            });

            //var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            //{
            //    Feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>() { id }, new List<IEnumerable<string>>()),
            //    AffectedFeatures = new List<NgisFeature>() { lineFeature }
            //});

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("Polygon", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));

            var references = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(references);
            Assert.Equal(references.First(), NgisFeatureHelper.GetLokalId(feature2));
        }

        [Fact]
        public void ReturnsLinesAndPolygonWhenCreatingPolygonFrom2Lines()
        {
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple ordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: true);
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: false);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings and check for Point Inside Area");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: false, insideCheck: true);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2Lines with multiple unordered linestrings and add a line that is not part of the polygon");
            GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(ordered: false, insideCheck: true, extraLines: true);

        }

        private List<NgisFeature>? GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered = true, bool insideCheck = false, bool extraLines = false)
        // private void GetLinesAndPolygonWhenCreatingPolygonFrom2Lines(bool ordered=true, double? x=null, double? y=null)
        {
            var id = Guid.NewGuid().ToString();

            var linestring = new LineString(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100)
            });

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);



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
            // Type = "Kaiomr�deGrense"
            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);


            Point? centroid = null;
            if (insideCheck)
            {
                // Check if polygon  entirely contains the given coordinate location
                centroid = new Point(new Coordinate(50, 50));

            }

            var features = new List<NgisFeature>();

            NgisFeature? lineFeatureExtra = null;
            if (extraLines)
            {
                // Add a line that is not part of the polygon
                var idExtra = Guid.NewGuid().ToString();
                var linestringExtra = new LineString(new[]
                {
                    new Coordinate(200, 200),
                    new Coordinate(300, 200),
                    new Coordinate(300, 300)
                });

                lineFeatureExtra = NgisFeatureHelper.CreateFeature(linestringExtra, idExtra, Operation.Create);
                features = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeatureExtra };
            }
            else
            {
                features = new List<NgisFeature>() { lineFeature, lineFeature2 };
            }

            //Type = "Kaiomr�de"

            // _topologyImplementation.CreatePolygonFromLines replaces _topologyImplementation.ResolveReferences
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroid = centroid
            });

            //var feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>() { id, id2 }, new List<IEnumerable<string>>());
            //var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            //{
            //    Feature = feature,
            //    AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2 }
            //});



            Assert.Equal(3, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon

            Assert.Equal("Polygon", featurePolygon.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));


            var feature1References = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(feature1References);
            Assert.Equal(feature1References.First(), NgisFeatureHelper.GetLokalId(featurePolygon));

            var feature2References = NgisFeatureHelper.GetExteriors(feature2);
            Assert.Single(feature2References);
            Assert.Equal(feature2References.First(), NgisFeatureHelper.GetLokalId(featurePolygon));


            var feature3References = NgisFeatureHelper.GetExteriors(featurePolygon);
            Assert.Equal(2, feature3References.Count);
            Assert.Equal(feature3References.First(), NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(feature3References.Last(), NgisFeatureHelper.GetLokalId(feature2));

            if (insideCheck)
            {
                output.WriteLine("InsideCheck for Point Inside Area:{0}", res.IsValid);
                // Assert.True(res.IsValid);
            }

            var poly = featurePolygon.Geometry;
            output.WriteLine(poly.ToString());
            output.WriteLine("");

            return res.AffectedFeatures;
        }

        [Fact]

        void CheckTopologyForRecreateAreaIsValid()
        {
            output.WriteLine("CheckTopologyForRecreateAreaIsValid valid geometries");
            Check2TopologyForRecreateArea(ordered: true, inputValid: true);

            output.WriteLine("CheckTopologyForRecreateAreaIsValid invalid geometries");
            Check2TopologyForRecreateArea(ordered: true, inputValid: false);
        }

        private void Check2TopologyForRecreateArea(bool ordered = true, bool inputValid = true)
        {
            var id = Guid.NewGuid().ToString();

            LineString linestring;
            if (inputValid)
            {
                linestring = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(100, 0),
                    new Coordinate(100, 100)
                });
            }
            else
            {
                linestring = new LineString(new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(100, 0),
                    new Coordinate(100, 200)
                });

            }

            //Type = "Kaiomr�deGrense"
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);


            var id2 = Guid.NewGuid().ToString();

            LineString linestring2;
            if (ordered)
            {
                if (inputValid)
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
                    linestring2 = new LineString(new[]
                    {
                        new Coordinate(200, 100),
                        new Coordinate(0, 100),
                        new Coordinate(0, 0)
                    });
                }
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

            //Type = "Kaiomr�deGrense"
            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);


            Point? centroid = null;

            // _topologyImplementation.CreatePolygonFromLines replaces _topologyImplementation.ResolveReferences
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = new List<NgisFeature>() { lineFeature, lineFeature2 },
                Centroid = centroid
            });

            //var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            //{
            //    //Type = "Kaiomr�de",
            //    //Centroid = centroid
            //    Feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>() { id, id2 }, new List<IEnumerable<string>>()),
            //    AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2 }
            //});

            bool hasValidPolygon = false;
            foreach (var feat in res.AffectedFeatures)
            {
                if (feat.Geometry != null && feat.Geometry.GeometryType == "Polygon")
                {
                    hasValidPolygon = true;
                    break;

                }
            }
            output.WriteLine("Valid input lines: {0}", hasValidPolygon);
        }

        [Fact]
        public void ReturnsLinesAndPolygonWhenCreatingPolygonFrom2LinesAndHoles()
        {
            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles with multiple ordered linestrings and input exteriors and interiors");
            GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(ordered: true, insideCheck: false, specifyInteriors: true);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles with multiple ordered linestrings and input linestrings only");
            GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(ordered: true, insideCheck: false, specifyInteriors: false);

        }

        private void GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(bool ordered = true, bool insideCheck = false, bool specifyInteriors = true)
        {
            var id = Guid.NewGuid().ToString();
            var linestring = new LineString(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100)
            });
            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);

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
            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);

            Point? centroid = null;
            if (insideCheck)
            {
                // Check if polygon  entirely contains the given coordinate location
                centroid = new Point(new Coordinate(50, 50));
            }

            // Add lines that forms a hole - HOLE 1
            var id3Hole1 = Guid.NewGuid().ToString();
            var linestring1Hole1 = new LineString(new[]
            {
                new Coordinate(25, 25),
                new Coordinate(50, 25),
                new Coordinate(50, 50)
            });
            var lineFeature1Hole1 = NgisFeatureHelper.CreateFeature(linestring1Hole1, id3Hole1, Operation.Create);
            // Line 2 to close the hole
            var id4Hole1 = Guid.NewGuid().ToString();
            var linestring2Hole1 = new LineString(new[]
            {
                new Coordinate(50, 50),
                new Coordinate(25, 50),
                new Coordinate(25, 25)
            });
            var lineFeature2Hole1 = NgisFeatureHelper.CreateFeature(linestring2Hole1, id4Hole1, Operation.Create);

            // Add lines that forms a hole - HOLE 2
            var id3Hole2 = Guid.NewGuid().ToString();
            var linestring1Hole2 = new LineString(new[]
            {
                    new Coordinate(55, 55),
                    new Coordinate(60, 55),
                    new Coordinate(60, 60)
                });
            var lineFeature1Hole2 = NgisFeatureHelper.CreateFeature(linestring1Hole2, id3Hole2, Operation.Create);
            // Line 2 to close the hole
            var id4Hole2 = Guid.NewGuid().ToString();
            var linestring2Hole2 = new LineString(new[]
            {
                    new Coordinate(60, 60),
                    new Coordinate(55, 60),
                    new Coordinate(55, 55)
                });
            var lineFeature2Hole2 = NgisFeatureHelper.CreateFeature(linestring2Hole2, id4Hole2, Operation.Create);



            NgisFeature feature;
            TopologyResponse res;
            if (specifyInteriors)
            {
                var interiors = new List<List<string>>();
                interiors.Add(new List<string>() { id3Hole1, id4Hole1 });
                interiors.Add(new List<string>() { id3Hole2, id4Hole2 });
                // specify bpthh exterior and interior
                feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>() { id, id2 }, interiors);
                //res = _topologyImplementation.CreatePolygonFromGeometry(new ToplogyRequest()
                res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
                {
                    Feature = feature,
                    AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeature1Hole1, lineFeature2Hole1, lineFeature1Hole2, lineFeature2Hole2 }

                });
            }
            else
            {
                // specify only exterior as linestrings, and let the topologyImplementation (NTS) fix the holes
                // _topologyImplementation.CreatePolygonFromLines replaces _topologyImplementation.ResolveReferences
                feature = NgisFeatureHelper.CreateFeature(new Polygon(null), null, Operation.Create, new List<string>() { id, id2, id3Hole1, id4Hole1, id3Hole2, id4Hole2 }, null);
                res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
                {
                    // Features = new List<NgisFeature>() { lineFeature, lineFeature2 },
                    Features = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeature1Hole1, lineFeature2Hole1, lineFeature1Hole2, lineFeature2Hole2 },
                    Centroid = centroid
                });

            }

            //var res = _topologyImplementation.ResolveReferences(new ToplogyRequest()
            //{
            //    Feature = feature,
            //    AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeature1Hole1, lineFeature2Hole1, lineFeature1Hole2, lineFeature2Hole2 }
            //});


            Assert.Equal(7, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));

            var featurePolygon = res.AffectedFeatures.ElementAt(6); // polygon
            Assert.Equal("Polygon", featurePolygon.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(featurePolygon));

            var feature1References = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(feature1References);
            Assert.Equal(feature1References.First(), NgisFeatureHelper.GetLokalId(featurePolygon));

            var feature2References = NgisFeatureHelper.GetExteriors(feature1);
            Assert.Single(feature2References);
            Assert.Equal(feature2References.First(), NgisFeatureHelper.GetLokalId(featurePolygon));

            var feature3References = NgisFeatureHelper.GetExteriors(featurePolygon);
            Assert.Equal(2, feature3References.Count);
            Assert.Equal(feature3References.First(), NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(feature3References.Last(), NgisFeatureHelper.GetLokalId(feature2));

            if (insideCheck)
            {
                output.WriteLine("InsideCheck for Point Inside Area:{0}", res.IsValid);
                // Assert.True(res.IsValid);
            }

            var featureHolesReferences = NgisFeatureHelper.GetInteriors(featurePolygon);
            foreach (var hole in featureHolesReferences)
            {
                Assert.Equal(featureHolesReferences.First().First(), NgisFeatureHelper.GetLokalId(lineFeature1Hole1));
                Assert.Equal(featureHolesReferences.First().Last(), NgisFeatureHelper.GetLokalId(lineFeature2Hole1));

                Assert.Equal(featureHolesReferences.Last().First(), NgisFeatureHelper.GetLokalId(lineFeature1Hole2));
                Assert.Equal(featureHolesReferences.Last().Last(), NgisFeatureHelper.GetLokalId(lineFeature2Hole2));

            }

            var poly = featurePolygon.Geometry;
            output.WriteLine(poly.ToString());


        }

        [Fact]
        public void MovePointOnReferencedLine()
        {
            // 1. Create polygons with referenced lines
            // 2. Move a point on the referenced line
            // 3. Return updated polygon.

            //// 1. Create polygons with referenced lines
            var affectedFeatures = GetLinesAndPolygonWhenCreatingPolygonFrom2Lines();
            var lineFeature1 = affectedFeatures.First();
            var lineFeature2 = affectedFeatures[1];
            var polygonFeature = affectedFeatures.Last();

            // 2. Move a point on the referenced line
            LineString lineStringModified = (LineString)lineFeature1.Geometry.Copy();
            lineStringModified[1].X += 10;
            lineStringModified[1].Y += 20;

            var movedCoordinate = new Coordinate() { X = lineStringModified[1].X, Y = lineStringModified[1].Y };
            var editedFeature = EditObject(affectedFeatures, EditOperation.Edit, lineFeature1, index: 1, newCoordinate: movedCoordinate);

            // 3. Return updated polygon.
            var features = new List<NgisFeature>() { editedFeature, lineFeature2 };
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroid = null
            });
            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon
            var poly = featurePolygon.Geometry;
            output.WriteLine("Modified polygon: " + poly.ToString());

        }

        [Fact]
        public void InsertPointOnReferencedLine()
        {
            // 1. Create polygons with referenced lines
            var affectedFeatures = GetLinesAndPolygonWhenCreatingPolygonFrom2Lines();
            var lineFeature1 = affectedFeatures.First();
            var lineFeature2 = affectedFeatures[1];
            var polygonFeature = affectedFeatures.Last();


            // Insert new point
            LineString lineStringModified = (LineString)lineFeature1.Geometry.Copy();
            lineStringModified[1].X += 10;
            lineStringModified[1].Y += 20;
            var insertCoordinateCoordinate = new Coordinate() { X = lineStringModified[1].X, Y = lineStringModified[1].Y };
            var editedFeature = EditObject(affectedFeatures, EditOperation.Insert, lineFeature1, index: 1, newCoordinate: insertCoordinateCoordinate);


            // 3. Return updated polygon.
            var features = new List<NgisFeature>() { editedFeature, lineFeature2 };
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroid = null
            });
            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon
            var poly = featurePolygon.Geometry;
            output.WriteLine("Modified polygon: " + poly.ToString());

        }

        [Fact]
        public void DeletePointOnReferencedLine()
        {
            // 1. Create polygons with referenced lines
            var affectedFeatures = GetLinesAndPolygonWhenCreatingPolygonFrom2Lines();
            var lineFeature1 = affectedFeatures.First();
            var lineFeature2 = affectedFeatures[1];
            var polygonFeature = affectedFeatures.Last();


            // delete existing point
            //LineString lineStringModified = (LineString)lineFeature1.Geometry.Copy();
            //lineStringModified[1].X += 10;
            //lineStringModified[1].Y += 20;
            //var insertCoordinateCoordinate = new Coordinate() { X = lineStringModified[1].X, Y = lineStringModified[1].Y };
            var editedFeature = EditObject(affectedFeatures, EditOperation.Delete, lineFeature1, index: 1);


            // 3. Return updated polygon.
            var features = new List<NgisFeature>() { editedFeature, lineFeature2 };
            var res = _topologyImplementation.CreatePolygonFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroid = null
            });
            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon
            var poly = featurePolygon.Geometry;
            output.WriteLine("Modified polygon: " + poly.ToString());

        }

        private NgisFeature? EditObject(List<NgisFeature> affectedFeatures, EditOperation editOperation, NgisFeature lineFeature, int index, Coordinate? newCoordinate = null)
        {
            switch (editOperation)
            {
                case EditOperation.Edit:
                    {
                        if (newCoordinate == null)
                        {
                            throw new Exception("Missing Coordinate value");
                        }

                        var currentLinestring = (LineString)lineFeature.Geometry;
                        var currentCoordinate = currentLinestring[index].CoordinateValue;
                        currentCoordinate.CoordinateValue = newCoordinate;
                        return lineFeature;
                    }

                case EditOperation.Delete:
                    {
                        var currentLinestring = (LineString)lineFeature.Geometry;
                        lineFeature.Geometry = DeletePoint(currentLinestring, index);
                        return lineFeature;
                    }

                case EditOperation.Insert:
                    {
                        if (newCoordinate == null)
                        {
                            throw new Exception("Missing Coordinate value");
                        }

                        var currentLinestring = (LineString)lineFeature.Geometry;
                        lineFeature.Geometry = InsertPoint(currentLinestring, index, newCoordinate);
                        return lineFeature;
                    }
            }

            return null;
        }

        Geometry InsertPoint(Geometry geom, int index, Coordinate newPoint)
        {

            var element = (LineString)geom;

            var oldSeq = element.CoordinateSequence;
            var newSeq = element.Factory.CoordinateSequenceFactory.Create(
                oldSeq.Count + 1, oldSeq.Dimension, oldSeq.Measures);

            if (index == 0)
            {
                // Before first point
                newSeq.SetCoordinate(0, newPoint);
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 1, oldSeq.Count);
            }
            else if (index == oldSeq.Count - 1)
            {
                // Last point
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, oldSeq.Count);
                newSeq.SetCoordinate(oldSeq.Count, newPoint);
            }
            else
            {
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, index + 1);
                newSeq.SetCoordinate(index + 1, newPoint);
                CoordinateSequences.Copy(oldSeq, index + 1, newSeq, index + 2, newSeq.Count - 2 - index);
            }

            var linestring = geom.Factory.CreateLineString(newSeq);
            return linestring;
        }

        Geometry DeletePoint(Geometry geom, int index)
        {
            var element = (LineString)geom;
            if (element.Count < 3)
            {
                throw new InvalidOperationException();
            }
            var oldSeq = element.CoordinateSequence;
            var newSeq = element.Factory.CoordinateSequenceFactory.Create(
                oldSeq.Count - 1, oldSeq.Dimension, oldSeq.Measures);

            if (index == 0)
            {
                // first point
                CoordinateSequences.Copy(oldSeq, 1, newSeq, 0, newSeq.Count);
            }
            else if (index == oldSeq.Count - 1)
            {
                // Last point
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, newSeq.Count);
            }
            else
            {
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, index);
                CoordinateSequences.Copy(oldSeq, index + 1, newSeq, index, newSeq.Count - index);
            }

            var linestring = geom.Factory.CreateLineString(newSeq);
            return linestring;



        }

    }
    internal static class ICoordinateSequenceEx
    {
        public static void SetCoordinate(this CoordinateSequence self, int index, Coordinate coord)
        {
            self.SetOrdinate(index, Ordinate.X, coord.X);
            self.SetOrdinate(index, Ordinate.Y, coord.Y);
            if (self.Dimension > 2) self.SetOrdinate(index, Ordinate.Z, coord.Z);
        }
    }
}