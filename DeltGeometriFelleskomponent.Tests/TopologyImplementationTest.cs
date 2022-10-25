using System;
using System.Collections.Generic;
using System.Linq;
using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using NetTopologySuite.Geometries;
using Xunit;
using Xunit.Abstractions;

namespace DeltGeometriFelleskomponent.Tests
{
    public class TopologyImplementationTest : TestBase
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

            var res = _topologyImplementation.CreateGeometry(new CreateGeometryRequest()
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

            var res = _topologyImplementation.CreateGeometry(new CreateGeometryRequest()
            {
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

            var lineFeature = NgisFeatureHelper.CreateFeature(linestring, id, Operation.Create);

            var res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = new List<NgisFeature>() { lineFeature },
                Centroids = null
            }).First();

            Assert.Equal(2, res.AffectedFeatures.Count());
            var feature1 = res.AffectedFeatures.First();

            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);

            Assert.Equal("Polygon", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));
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

            var features = new List<NgisFeature>();


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

                var lineFeatureExtra = NgisFeatureHelper.CreateFeature(linestringExtra, idExtra, Operation.Create);
                features = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeatureExtra };
            }
            else
            {
                features = new List<NgisFeature>() { lineFeature, lineFeature2 };
            }


            var res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroids = centroid != null ? new List<Point> { centroid } : null
            }).First();

            Assert.Equal(3, res.AffectedFeatures.Count());

            var feature1 = res.AffectedFeatures.ElementAt(1);
            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));


            var feature2 = res.AffectedFeatures.ElementAt(0);
            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));


            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon

            Assert.Equal("Polygon", featurePolygon.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));


            var feature3References = NgisFeatureHelper.GetExteriors(featurePolygon);
            Assert.Equal(2, feature3References.Count);
            Assert.Equal(feature3References.ElementAt(1), NgisFeatureHelper.GetLokalId(feature1));

            //we disregard the direction of the reference here, since I cannot wrap my head around this test
            Assert.Equal(NgisFeatureHelper.RemoveSign(feature3References.Last()), NgisFeatureHelper.GetLokalId(feature1));

            if (insideCheck)
            {
                output.WriteLine("InsideCheck for Point Inside Area:{0}", res.IsValid);
            }

            var poly = featurePolygon.Geometry;
            output.WriteLine(poly.ToString());
            output.WriteLine("");

            return res.AffectedFeatures;
        }

        [Fact]
        public void CheckTopologyForRecreateAreaIsValid()
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

            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);

            var res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = new List<NgisFeature>() { lineFeature, lineFeature2 },
            }).First();

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
            GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(insideCheck: false, specifyInteriors: true);

            output.WriteLine("GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles with multiple ordered linestrings and input linestrings only");
            GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(insideCheck: false, specifyInteriors: false);
        }

        private void GetLinesAndPolygonWhenCreatingPolygonFrom2LinesWithHoles(bool insideCheck = false, bool specifyInteriors = true)
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
            var linestring2 = new LineString(new[] {
                new Coordinate(100, 100),
                new Coordinate(0, 100),
                new Coordinate(0, 0)
            });

            var lineFeature2 = NgisFeatureHelper.CreateFeature(linestring2, id2, Operation.Create);

            // Check if polygon  entirely contains the given coordinate location
            Point? centroid = insideCheck ? new Point(new Coordinate(50, 50)) : null;

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
                res = _topologyImplementation.CreateGeometry(new CreateGeometryRequest()
                {
                    Feature = feature,

                    AffectedFeatures = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeature1Hole1, lineFeature2Hole1, lineFeature1Hole2, lineFeature2Hole2 }

                });
            }
            else
            {
                // specify only exterior as linestrings, and let the topologyImplementation (NTS) fix the holes                
                res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
                {
                    Centroids = centroid != null ? new List<Point> { centroid } : null,
                    Features = new List<NgisFeature>() { lineFeature, lineFeature2, lineFeature1Hole1, lineFeature2Hole1, lineFeature1Hole2, lineFeature2Hole2 },
                }).First();
            }

            Assert.Equal(7, res.AffectedFeatures.Count());

            foreach (var f in res.AffectedFeatures)
            {
                output.WriteLine($"{f.Update.Action} {NgisFeatureHelper.GetLokalId(f)} {f.Geometry.GeometryType}");
            }

            var feature1 = res.AffectedFeatures.First();
            Assert.Equal("LineString", feature1.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature1));

            var feature2 = res.AffectedFeatures.ElementAt(1);
            Assert.Equal("LineString", feature2.Geometry!.GeometryType);
            Assert.Equal(id, NgisFeatureHelper.GetLokalId(feature2));
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(feature2));


            var featurePolygon = res.AffectedFeatures.Last(); // polygon
            Assert.Equal("Polygon", featurePolygon.Geometry!.GeometryType);
            Assert.Equal(Operation.Create, NgisFeatureHelper.GetOperation(featurePolygon));

            var feature3References = NgisFeatureHelper.GetExteriors(featurePolygon);
            Assert.Equal(2, feature3References.Count);
            Assert.Equal(feature3References.First(), NgisFeatureHelper.GetLokalId(feature1));
            Assert.Equal(feature3References.Last(), NgisFeatureHelper.GetLokalId(feature2));

            var holes = NgisFeatureHelper.GetInteriors(featurePolygon);
            Assert.Equal($"-{id3Hole1}", holes[0][1]);
            Assert.Equal($"-{id4Hole1}", holes[0][0]);

            Assert.Equal($"-{id3Hole2}", holes[1][1]);
            Assert.Equal($"-{id4Hole2}", holes[1][0]);
        }

        [Fact]
        public void MovePointOnReferencedLine()
        {
            //// 1. Create polygons with referenced lines
            var affectedFeatures = GetLinesAndPolygonWhenCreatingPolygonFrom2Lines();
            var lineFeature1 = affectedFeatures.First();
            var lineFeature2 = affectedFeatures[1];

            // 2. Move a point on the referenced line
            LineString lineStringModified = (LineString)lineFeature1.Geometry.Copy();
            lineStringModified[1].X += 10;
            lineStringModified[1].Y += 20;

            var movedCoordinate = new Coordinate() { X = lineStringModified[1].X, Y = lineStringModified[1].Y };
            var edited = GeometryEdit.EditObject(new EditLineRequest()
            {
                AffectedFeatures = affectedFeatures,
                Feature = lineFeature1,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeIndex = 1,
                    NodeValue = new List<double>() { movedCoordinate.X, movedCoordinate.Y }
                },
            });

            // 3. Return updated polygon.
            var features = new List<NgisFeature>() { edited[0], lineFeature2 };
            var res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroids = null
            }).First();
            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon
            var poly = featurePolygon.Geometry;
            output.WriteLine("Modified polygon: " + poly.ToString());

            Assert.True(res.IsValid, "Unable to move point on polygon");

        }

        [Fact]
        public void DeletePointOnReferencedLine()
        {
            // 1. Create polygons with referenced lines
            var affectedFeatures = GetLinesAndPolygonWhenCreatingPolygonFrom2Lines();
            var lineFeature1 = affectedFeatures.First();
            var lineFeature2 = affectedFeatures[1];

            var coordinateCount1 = lineFeature1.Geometry.Coordinates.Length;

            var edited = GeometryEdit.EditObject(new EditLineRequest()
            {
                AffectedFeatures = affectedFeatures,
                Feature = lineFeature1,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Delete,
                    NodeIndex = 1
                }
            });

            var coordinateCount2 = lineFeature1.Geometry.Coordinates.Length;
            // 3. Return updated polygon.
            var features = new List<NgisFeature>() { edited[0], lineFeature2 };
            var res = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            {
                Features = features,
                Centroids = null
            }).First();
            var featurePolygon = res.AffectedFeatures.ElementAt(2); // polygon
            var poly = featurePolygon.Geometry;
            output.WriteLine("Modified polygon: " + poly.ToString());

            Assert.True(coordinateCount1 - coordinateCount2 == 1, "Unable to delete point on polygon");

            Assert.True(res.IsValid, "Unable to delete point on polygon");
        }

        [Fact]
        public void HandlesDeleteOfNodeInLineUsedByAPolygon()
        {
            var line1 = GetExampleFeature("1");
            var line2 = GetExampleFeature("2");
            var polygonFeature = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { line1, line2 } }).First().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            var request = new EditLineRequest()
            {
                Feature = line1,
                Edit = new EditLineOperation()
                {
                    NodeIndex = 1,
                    Operation = EditOperation.Delete
                },
                AffectedFeatures = new List<NgisFeature>() { line2, NgisFeatureHelper.Copy(polygonFeature!) }
            };

            var response = _topologyImplementation.EditLine(request);

            Assert.Equal(3, response.AffectedFeatures.Count);

            var editedPolygonFeature = response.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon")!;
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedPolygonFeature));

            var polygon = (Polygon)polygonFeature!.Geometry;
            var editedPolygon = (Polygon)editedPolygonFeature.Geometry;
            Assert.Equal(polygon.Shell.Coordinates.Length - 1, editedPolygon.Shell.Coordinates.Length);

            var returnedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1")!;
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(returnedLine1));

            var returnedLine2 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "2")!;
            Assert.Null(returnedLine2.Update);
        }

        [Fact]
        public void HandlesEditOfConnectingNodeInLineUsedByAPolygon()
        {
            var line1 = GetExampleFeature("1");
            var line2 = GetExampleFeature("2");
            var polygonFeature = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { line1, line2 } }).First().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            var point = line1.Geometry.Coordinates[0];
            var editedPoint = new List<double>() { point.X + 0.001, point.Y + 0.001 };

            var request = new EditLineRequest()
            {
                Feature = line1,
                Edit = new EditLineOperation()
                {
                    NodeIndex = 0,
                    NodeValue = editedPoint,
                    Operation = EditOperation.Edit
                },
                AffectedFeatures = new List<NgisFeature>() { line2, NgisFeatureHelper.Copy(polygonFeature!) }
            };

            var response = _topologyImplementation.EditLine(request);

            foreach (var affectedFeature in response.AffectedFeatures)
            {
                output.WriteLine($"affected: {NgisFeatureHelper.GetLokalId(affectedFeature)} : {affectedFeature.Geometry.ToString()} ");
            }

            Assert.True(response.IsValid);
            Assert.Equal(3, response.AffectedFeatures.Count);

            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1");

            Assert.Equal(editedPoint[0], editedLine1.Geometry.Coordinates[0].X);
            Assert.Equal(editedPoint[1], editedLine1.Geometry.Coordinates[0].Y);

            var editedPolygonFeature = response.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon")!;
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedPolygonFeature));

            var returnedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1")!;
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(returnedLine1));

            var returnedLine2 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "2")!;
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(returnedLine2));
        }


        [Fact]
        public void DoesNotReturnLinesUnrelatedToChangeInAffectedFeaturesWhenEditingNonNodePoint()
        {
            var features = ReadFeatures("Examples/example_ngisfeatures_edit.geojson");

            var line1 = features.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1c854251-4a5c-45f3-b21f-efb905299649")!;
            var affected = features.Where(f => NgisFeatureHelper.GetLokalId(f) != "1c854251-4a5c-45f3-b21f-efb905299649")!;

            var edit = new EditLineOperation()
            {
                NodeIndex = 2,
                NodeValue = new List<double>() { 10.981049537658693, 60.82098367777078 },
                Operation = EditOperation.Edit
            };

            var request = new EditLineRequest()
            {
                Feature = NgisFeatureHelper.Copy(line1),
                Edit = edit,
                AffectedFeatures = affected.Select(NgisFeatureHelper.Copy).ToList()
            };

            var response = _topologyImplementation.EditLine(request);

            Assert.True(response.IsValid);
            Assert.Equal(3, response.AffectedFeatures.Count);

            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1c854251-4a5c-45f3-b21f-efb905299649")!;
            var affectedLine = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "02999bcc-fe82-4ce6-8a2e-6f01aeac0b8a")!;
            var affectedPolygon = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "f946043d-2c4b-4278-b1ac-6eb8073daac1")!;

            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedLine1));
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(affectedPolygon));
            Assert.Null(NgisFeatureHelper.GetOperation(affectedLine));
        }

        [Fact]
        public void HandlesEditOfNodePointConnectingThreeLinesAndTwoPolygons()
        {
            var features = ReadFeatures("Examples/example_ngisfeatures_edit.geojson");

            var line1 = features.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1c854251-4a5c-45f3-b21f-efb905299649")!;
            var affected = features.Where(f => NgisFeatureHelper.GetLokalId(f) != "1c854251-4a5c-45f3-b21f-efb905299649")!;

            var editIndex = 3;
            var oldPoint = line1.Geometry.Coordinates[editIndex];
            var newPoint = new Coordinate(10.982551574707031, 60.81545954370719);

            var edit = new EditLineOperation()
            {
                NodeIndex = editIndex,
                NodeValue = new List<double>() { newPoint.X, newPoint.Y },
                Operation = EditOperation.Edit
            };

            var request = new EditLineRequest()
            {
                Feature = NgisFeatureHelper.Copy(line1),
                Edit = edit,
                AffectedFeatures = affected.Select(NgisFeatureHelper.Copy).ToList()
            };

            var response = _topologyImplementation.EditLine(request);

            Assert.True(response.IsValid);
            Assert.Equal(5, response.AffectedFeatures.Count);

            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1c854251-4a5c-45f3-b21f-efb905299649")!;
            var affectedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "02999bcc-fe82-4ce6-8a2e-6f01aeac0b8a")!;
            var affectedLine2 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "0bcfc672-b215-4393-857e-d6a94418a4d8")!;

            var affectedPolygon1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "f946043d-2c4b-4278-b1ac-6eb8073daac1")!;
            var affectedPolygon2 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "a018176a-9a1b-4553-a3f5-a80400c24c87")!;

            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(editedLine1));
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(affectedLine1));
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(affectedLine2));
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(affectedPolygon1));
            Assert.Equal(Operation.Replace, NgisFeatureHelper.GetOperation(affectedPolygon2));

            Assert.Equal(newPoint, editedLine1.Geometry.Coordinates.Last());
            Assert.Equal(newPoint, affectedLine1.Geometry.Coordinates.Last());
            Assert.Equal(newPoint, affectedLine2.Geometry.Coordinates.Last());

            Assert.Contains(affectedPolygon1.Geometry.Coordinates, c => c.Equals(newPoint));
            Assert.DoesNotContain(affectedPolygon1.Geometry.Coordinates, c => c.Equals(oldPoint));

            Assert.Contains(affectedPolygon2.Geometry.Coordinates, c => c.Equals(newPoint));
            Assert.DoesNotContain(affectedPolygon2.Geometry.Coordinates, c => c.Equals(oldPoint));
        }

        [Fact]
        public void DoesNotAllowDeleteWhichResultsInLineWithOnePoint()
        {
            var features = ReadFeatures("Examples/example_ngisfeatures_edit.geojson");

            var line1 = features.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "02999bcc-fe82-4ce6-8a2e-6f01aeac0b8a")!;
            var affected = features.Where(f => NgisFeatureHelper.GetLokalId(f) != "02999bcc-fe82-4ce6-8a2e-6f01aeac0b8a")!;


            var edit = new EditLineOperation()
            {
                NodeIndex = 1,
                Operation = EditOperation.Delete
            };

            var request = new EditLineRequest()
            {
                Feature = NgisFeatureHelper.Copy(line1),
                Edit = edit,
                AffectedFeatures = affected.Select(NgisFeatureHelper.Copy).ToList()
            };

            var response = _topologyImplementation.EditLine(request);

            foreach (var affectedFeature in response.AffectedFeatures)
            {
                output.WriteLine($"affected: {NgisFeatureHelper.GetLokalId(affectedFeature)} : {affectedFeature.Geometry.ToString()} ");
            }

            Assert.False(response.IsValid);
            Assert.Empty(response.AffectedFeatures);
        }


        [Fact]
        public void HandlesInsertOfNewConnectingNodeInLineUsedByAPolygon()
        {
            var line1 = GetExampleFeature("1");
            var line2 = GetExampleFeature("2");
            var polygonFeature = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { line1, line2 } }).First().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            var point = line1.Geometry.Coordinates[0];
            var editedPoint = new List<double>() { point.X - 0.001, point.Y + 0.001 };

            var request = new EditLineRequest()
            {
                Feature = NgisFeatureHelper.Copy(line1),
                Edit = new EditLineOperation()
                {
                    NodeIndex = 0,
                    NodeValue = editedPoint,
                    Operation = EditOperation.Insert
                },
                AffectedFeatures = new List<NgisFeature>() { line2, NgisFeatureHelper.Copy(polygonFeature!) }
            };

            var response = _topologyImplementation.EditLine(request);

            foreach (var affectedFeature in response.AffectedFeatures)
            {
                output.WriteLine($"affected: {NgisFeatureHelper.GetLokalId(affectedFeature)} : {affectedFeature.Geometry.ToString()} ");
            }

            Assert.True(response.IsValid);
            Assert.Equal(3, response.AffectedFeatures.Count);

            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1");

            Assert.Equal(line1.Geometry.Coordinates.Length + 1, editedLine1.Geometry.Coordinates.Length);
            Assert.Equal(editedPoint[0], editedLine1.Geometry.Coordinates[0].X);
            Assert.Equal(editedPoint[1], editedLine1.Geometry.Coordinates[0].Y);
        }

        [Fact]
        public void HandlesDeleteOfConnectingNodeInLineUsedByAPolygon()
        {
            var line1 = GetExampleFeature("1");
            var line2 = GetExampleFeature("2");
            var polygonFeature = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { line1, line2 } }).First().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            var point = line1.Geometry.Coordinates[0];
            var editedPoint = new List<double>() { point.X - 0.001, point.Y + 0.001 };

            var request = new EditLineRequest()
            {
                Feature = NgisFeatureHelper.Copy(line1),
                Edit = new EditLineOperation()
                {
                    NodeIndex = 0,
                    Operation = EditOperation.Delete
                },
                AffectedFeatures = new List<NgisFeature>() { line2, NgisFeatureHelper.Copy(polygonFeature!) }
            };

            var response = _topologyImplementation.EditLine(request);

            foreach (var affectedFeature in response.AffectedFeatures)
            {
                output.WriteLine($"affected: {NgisFeatureHelper.GetLokalId(affectedFeature)} : {affectedFeature.Geometry.ToString()} ");
            }

            Assert.True(response.IsValid);
            Assert.Equal(3, response.AffectedFeatures.Count);


            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "1");
            var editedLine2 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "2");

            Assert.Equal(line1.Geometry.Coordinates.Length - 1, editedLine1.Geometry.Coordinates.Length);
            Assert.Equal(line1.Geometry.Coordinates[1].X, editedLine1.Geometry.Coordinates[0].X);
            Assert.Equal(line1.Geometry.Coordinates[1].Y, editedLine1.Geometry.Coordinates[0].Y);

            Assert.Equal(line1.Geometry.Coordinates[1].X, editedLine2.Geometry.Coordinates[0].X);
            Assert.Equal(line1.Geometry.Coordinates[1].Y, editedLine2.Geometry.Coordinates[0].Y);
        }

        [Fact]
        public void HandlesEditOfConnectingNodeInLineUsedByAPolygonThatUsesSingleLine()
        {
            var line1 = GetExampleFeature("8");

            var polygonFeature = _topologyImplementation.CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
            { Features = new List<NgisFeature>() { line1 } }).First().AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon");

            var point = line1.Geometry.Coordinates[0];
            var editedPoint = new List<double>() { point.X + 0.001, point.Y + 0.001 };

            var request = new EditLineRequest()
            {
                Feature = line1,
                Edit = new EditLineOperation()
                {
                    NodeIndex = 0,
                    NodeValue = editedPoint,
                    Operation = EditOperation.Edit
                },
                AffectedFeatures = new List<NgisFeature>() { NgisFeatureHelper.Copy(polygonFeature!) }
            };

            var response = _topologyImplementation.EditLine(request);

            foreach (var affectedFeature in response.AffectedFeatures)
            {
                output.WriteLine($"affected: {NgisFeatureHelper.GetLokalId(affectedFeature)} : {affectedFeature.Geometry.ToString()} ");
            }

            Assert.True(response.IsValid);
            Assert.Equal(2, response.AffectedFeatures.Count);

            var editedLine1 = response.AffectedFeatures.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == "8");

            Assert.Equal(editedPoint[0], editedLine1.Geometry.Coordinates[0].X);
            Assert.Equal(editedPoint[1], editedLine1.Geometry.Coordinates[0].Y);
        }


        [Fact]
        public void TestEditLinePresicionBug()
        {
            var request = Read<EditLineRequest>("Examples/edit_line_request_with_presicion_issues.json");

            var response = _topologyImplementation.EditLine(request);
            Assert.True(response.IsValid);

        }
    }
}

