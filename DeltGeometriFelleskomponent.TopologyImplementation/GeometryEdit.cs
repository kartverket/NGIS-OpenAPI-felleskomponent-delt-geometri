using System.Linq;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;


namespace DeltGeometriFelleskomponent.TopologyImplementation
{
    public class GeometryEdit
    {
        private static readonly PolygonCreator _polygonCreator = new();

        public static TopologyResponse EditLine(EditLineRequest request)
        {
            if (request.Feature.Geometry.GeometryType != "LineString")
            {
                throw new ArgumentException("Can only edit line features");
            }

            var res = EditObject(request);

            if (res.Count == 0)
            {
                return new TopologyResponse()
                {
                    AffectedFeatures = new List<NgisFeature>() { },
                    IsValid = false
                };
            }

            //get all polygons in affected features
            var polygons = res.FindAll(f => f.Geometry.GeometryType == "Polygon");

            if (polygons.Count == 0)
            {
                return new TopologyResponse()
                {
                    AffectedFeatures = res,
                    IsValid = true
                };
            }
            
            var lineFeatures = res.FindAll(f => f.Geometry.GeometryType != "Polygon");
            
            //for each of the polygons, rebuild geometry
            var editedPolygons = polygons.Select(p =>
            {
                var references = GetReferencedFeatures(p, lineFeatures);
                var created = _polygonCreator.CreatePolygonFromLines(references.ToList(), null ).FirstOrDefault();


                if (created == null)
                {
                    return null;
                }

                if (!created.IsValid)
                {
                    return null;
                }

                var geometry = created.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon")?.Geometry;
                if (geometry == null)
                {
                    return null;
                }
                p.Geometry = geometry;
                return p;
            });

            var isValid = editedPolygons.All(p => p != null);
            var affectedFeatures = isValid
                ? polygons
                    .Select(polygon =>
                        GetReferencedFeatures(polygon, lineFeatures).Concat(new List<NgisFeature>() { polygon }))
                    .SelectMany(p => p)
                    .Select(f => NgisFeatureHelper.SetOperation2(f, Operation.Replace)).ToList()
                : new List<NgisFeature>();

            return new TopologyResponse()
            {
                AffectedFeatures = affectedFeatures,
                IsValid = isValid
            };
        }

        private static void SetEditOperation(EditLineRequest request)
        {
            if (request.NewFeature == null) return;

            request.Edit.NodeIndex = -1;
            
            SetDifference(request);
        }

        private static void SetDifference(EditLineRequest request)
        {            
            switch (request.Edit.Operation)
            {
                case (EditOperation.Insert):
                case (EditOperation.Edit):
                    {
                        var newCoordinate = request.NewFeature!.Geometry.Coordinates.Except(request.Feature.Geometry.Coordinates).First();

                        request.Edit.NodeValue = new List<double> { newCoordinate.X, newCoordinate.Y };

                        request.Edit.NodeIndex = request.NewFeature.Geometry.Coordinates.ToList().IndexOf(newCoordinate);

                        break;
                    }
                case (EditOperation.Delete):
                    {
                        // Should work, but won't :(
                        //return request.Feature.Geometry.Difference(request.NewFeature!.Geometry);

                        var missingOrMovedCoordinate = request.Feature.Geometry.Coordinates.Except(request.NewFeature!.Geometry.Coordinates).First();

                        request.Edit.NodeValue = new List<double> { missingOrMovedCoordinate.X, missingOrMovedCoordinate.Y };

                        request.Edit.NodeIndex = request.Feature.Geometry.Coordinates.ToList().IndexOf(missingOrMovedCoordinate);

                        break;
                    }
                default:
                    {
                        throw new Exception("Operation not supported");
                    }
            }
        }

        public static List<NgisFeature> EditObject(EditLineRequest request)
        {
            SetEditOperation(request);

            var affectedFeatures = request.AffectedFeatures;
            var lineFeature = request.Feature;
            var index = request.Edit.NodeIndex;

            var originalGeometry = (LineString)lineFeature.Geometry.Copy();
            lineFeature = ApplyChange(lineFeature, request.Edit);

            var coordinates = originalGeometry.Coordinates;
            

            if (IsEdgePoint(request.Edit, coordinates))
            {
                //This may be an edge point

                //in the case of an insert, the supplied index may be outside the existing geometry, so 
                //we cannot use that to find connecting points
                var exsistingIndex = request.Edit.Operation == EditOperation.Insert && index > coordinates.Length - 1 
                    ? index - 1 
                    : index;

                //if the line is a single line making up a polygon, we have to consider the line itself for connecting points
                var connects = IsSingleLineForPoint(request) 
                    ? GetConnectingPoint(new List<NgisFeature>() { request.Feature }, originalGeometry, exsistingIndex) 
                    : GetConnectingPoint(affectedFeatures, originalGeometry, exsistingIndex);

                if (connects != null)
                {
                    //remove the connected feature
                    affectedFeatures = affectedFeatures.Where(f =>
                        NgisFeatureHelper.GetLokalId(f) != NgisFeatureHelper.GetLokalId(connects.Feature)).ToList();

                    //and rebuild it
                    //this is going to get hairy when we get several connecting lines to a point
                    //but that is a problem for another day!
                    if (request.Edit.Operation == EditOperation.Delete)
                    {
                        var edited = originalGeometry.Coordinates[index == 0 ? 1 : coordinates.Length - 2];
                        var newPos = new List<double>(){ edited.X,edited.Y };
                        affectedFeatures.Add(ApplyChange(connects.Feature, new EditLineOperation()
                        {
                            NodeValue = newPos,
                            Operation = EditOperation.Edit,
                            NodeIndex = connects.Index
                        }));
                    }
                    else
                    {
                        affectedFeatures.Add(ApplyChange(connects.Feature, new EditLineOperation()
                        {
                            NodeValue = request.Edit.NodeValue,
                            Operation = EditOperation.Edit,
                            NodeIndex = connects.Index
                        }));
                    }
                }
            }
            
            return new List<NgisFeature>() { lineFeature }.Concat(affectedFeatures).ToList();
        }

        private static bool IsSingleLineForPoint(EditLineRequest request)
        {
            if (request.AffectedFeatures.Count == 1 && request.AffectedFeatures[0].Geometry.GeometryType == "Polygon")
            {
                var references = NgisFeatureHelper.GetAllReferences(request.AffectedFeatures[0]);
                return references.Count == 1 && references[0] == NgisFeatureHelper.GetLokalId(request.Feature);
            }

            return false;
        }

        private static bool IsEdgePoint(EditLineOperation edit, IReadOnlyCollection<Coordinate> coordinates)
        {
            if (edit.Operation == EditOperation.Insert)
            {
                //why? because an insert at the last index inserts before that point. So, in order to be an
                //edge the insert has to be at maxIndex+1 (also known as count)
                return edit.NodeIndex == 0 || edit.NodeIndex == coordinates.Count ;
            }

            return edit.NodeIndex == 0 || edit.NodeIndex == coordinates.Count - 1;
        }

        private static NgisFeature ApplyChange(NgisFeature lineFeature, EditLineOperation edit)
        {
            var existingGeometry = (LineString)lineFeature.Geometry.Copy();
            var editOperation = edit.Operation;
            lineFeature.Geometry = editOperation switch
            {
                EditOperation.Edit => ReplaceNode(existingGeometry, edit.NodeIndex, edit.NodeCoordinate),
                EditOperation.Delete => DeletePoint(existingGeometry, edit.NodeIndex),
                EditOperation.Insert => InsertPoint(existingGeometry, edit.NodeIndex, edit.NodeCoordinate),
            };
            return lineFeature;
        }

        private static ConnectingPoint? GetConnectingPoint(List<NgisFeature> affectedFeatures, LineString line, int index)
        {
            var coordinate = line.Coordinates[index];
            var buffered = new Point(coordinate).Buffer(line.Length * 0.001);
            var candidates = affectedFeatures.Where(f => f.Geometry.GeometryType == "LineString").Where(f => f.Geometry.Intersects(buffered));

            foreach (var candidate in candidates)
            {
                if (candidate.Geometry.Coordinates.First().Equals2D(coordinate))
                {
                    return new ConnectingPoint() { Index = 0, Feature = candidate };
                }
                if (candidate.Geometry.Coordinates.Last().Equals2D(coordinate))
                {
                    return new ConnectingPoint() { Index = candidate.Geometry.Coordinates.Length - 1, Feature = candidate };
                }
            }

            return null;
        }


        private static LineString ReplaceNode(LineString line, int index, Coordinate? newValue)
        {
            if (newValue == null)
            {
                throw new Exception("Missing Coordinate value");
            }

            var currentCoordinate = line[index].CoordinateValue;
            currentCoordinate.CoordinateValue = newValue;
            return line;
        }

        private static Geometry InsertPoint(Geometry geom, int index, Coordinate? newPoint)
        {
            if (newPoint == null)
            {
                throw new Exception("Missing Coordinate value");
            }

            
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
            else if (index == oldSeq.Count)
            {
                // Last point
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, oldSeq.Count);
                newSeq.SetCoordinate(oldSeq.Count, newPoint);
            }
            else
            {
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, index );
                newSeq.SetCoordinate(index , newPoint);
                CoordinateSequences.Copy(oldSeq, index , newSeq, index +1 , newSeq.Count - 1 - index);
            }
            

            var linestring = geom.Factory.CreateLineString(newSeq);
            return linestring;
        }

        private static Geometry DeletePoint(Geometry geom, int index)
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

        private static IEnumerable<NgisFeature> GetReferencedFeatures(NgisFeature feature, List<NgisFeature> candidates)
            => NgisFeatureHelper.GetAllReferences(feature).Select(lokalId => candidates.Find(f => NgisFeatureHelper.GetLokalId(f) == lokalId)).OfType<NgisFeature>();

    }

    internal class ConnectingPoint
    {
        public int Index { get; set; }
        public NgisFeature Feature { get; set; }
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
