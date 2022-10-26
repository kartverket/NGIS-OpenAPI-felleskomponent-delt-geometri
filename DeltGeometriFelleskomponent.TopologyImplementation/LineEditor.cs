using DeltGeometriFelleskomponent.Models;
﻿using DeltGeometriFelleskomponent.Models.Exceptions;
using NetTopologySuite.Geometries;


namespace DeltGeometriFelleskomponent.TopologyImplementation
{
    public class LineEditor
    {
        private static readonly PolygonCreator _polygonCreator = new();

        public static TopologyResponse EditLine(EditLineRequest request)
        {
            if (request.Feature.Geometry.GeometryType != "LineString")
            {
                throw new BadRequestException("Can only edit line features");
            }

            //apply the edit, get back all features directly affected

            var editedLines = new List<NgisFeature>();
            try { 
                 editedLines = EditObject(request);
                if (editedLines.Count == 0 || editedLines.Any(f => !f.Geometry.IsValid))
                {
                    return EmptyInvalidResponse();
                }
            }
            catch (InvalidOperationException)
            {
                return EmptyInvalidResponse();
            }

            var affectedIds = editedLines.Select(NgisFeatureHelper.GetLokalId).Distinct();

            //find all polygons affected by these changes
            var affectedPolygons = request.AffectedFeatures
                .Where(f => f.Geometry.GeometryType == "Polygon")
                .Where(f => NgisFeatureHelper.GetAllReferences(f).Any(r => affectedIds.Contains(r)));

            if (!affectedPolygons.Any())
            {
                return new TopologyResponse()
                {
                    AffectedFeatures = editedLines,
                    IsValid = true
                };
            }

            //find all references from the affected polygons to lines, since we have to return all these
            var polygonReferences = affectedPolygons.Select(NgisFeatureHelper.GetAllReferences).SelectMany(ids => ids).ToList();

            var affectedLines = request.AffectedFeatures
                .Where(f => f.Geometry.GeometryType == "LineString")
                .Where(f => !affectedIds.Contains(NgisFeatureHelper.GetLokalId(f)))
                .Concat(editedLines)
                .Where(f => polygonReferences.Contains(NgisFeatureHelper.GetLokalId(f)));

            var editedPolygons = affectedPolygons.Select(p => RecreatePolygon(p, GetReferencedFeatures(p, affectedLines)));

            var isValid = editedPolygons.All(p => p != null);

            return isValid 
                ? new TopologyResponse()
                {
                    AffectedFeatures = affectedLines.Concat(editedPolygons).ToList()!,
                    IsValid = true
                }
                : EmptyInvalidResponse();

        }

        private static void SetEditOperationAndNodeInfo(EditLineRequest request)
        {
            if (request.NewFeature == null) {
                if (request.Edit == null) throw new BadRequestException("Edit operation required if NewFeature not set.");
                return;
            }

            if(request.Edit != null)
            {
                SetDifference(request);

                return;
            }

            var newCoordinates = request.NewFeature!.Geometry.Coordinates.Except(request.Feature.Geometry.Coordinates).ToList();
            
            var missingOrMovedCoordinates = request.Feature.Geometry.Coordinates.Except(request.NewFeature!.Geometry.Coordinates).ToList();

            request.Edit = new EditLineOperation();

            if (newCoordinates.Count > 1 || missingOrMovedCoordinates.Count > 1) throw new NotImplementedException("Multi-node edits not implemented.");

            if (newCoordinates.Any()) {
                request.Edit.Operation = missingOrMovedCoordinates.Any() 
                     ? EditOperation.Edit
                     : EditOperation.Insert;

                SetNodeInfo(request.Edit, newCoordinates.First(), request.NewFeature);
            }
            else if (missingOrMovedCoordinates.Any()) {
                request.Edit.Operation = EditOperation.Delete;

                SetNodeInfo(request.Edit, missingOrMovedCoordinates.First(), request.Feature);
            }
            else throw new BadRequestException("No difference detected between geometries");
        }

        private static void SetDifference(EditLineRequest request)
        {
            switch (request.Edit!.Operation)
            {
                case EditOperation.Insert:
                case EditOperation.Edit:
                    {
                        var newCoordinate = request.NewFeature!.Geometry.Coordinates.Except(request.Feature.Geometry.Coordinates).First();

                        SetNodeInfo(request.Edit, newCoordinate, request.NewFeature);

                        break;
                    }
                case EditOperation.Delete:
                    {
                        // Should work, but won't :(
                        //return request.Feature.Geometry.Difference(request.NewFeature!.Geometry);

                        var missingOrMovedCoordinate = request.Feature.Geometry.Coordinates.Except(request.NewFeature!.Geometry.Coordinates).First();

                        SetNodeInfo(request.Edit, missingOrMovedCoordinate, request.Feature);

                        break;
                    }
                default:
                    {
                        throw new BadRequestException($"Operation {request.Edit.Operation} not supported");
                    }
            }
        }

        private static void SetNodeInfo(EditLineOperation edit, Coordinate coordinate, NgisFeature feature)
        {
            edit.NodeValue = new List<double> { coordinate.X, coordinate.Y };

            edit.NodeIndex = feature.Geometry.Coordinates.ToList().IndexOf(coordinate);
        }

        public static List<NgisFeature> EditObject(EditLineRequest request)
        {
            SetEditOperationAndNodeInfo(request);

            var affectedFeatures = request.AffectedFeatures ?? new List<NgisFeature>();
            var lineFeature = request.Feature;
            var index = request.Edit!.NodeIndex;

            var originalGeometry = (LineString)lineFeature.Geometry.Copy();
            var editedLineFeature = ApplyChange(lineFeature, request.Edit);
            

            var coordinates = originalGeometry.Coordinates;

            var modifiedFeatures = new List<NgisFeature>() { editedLineFeature };

            if (IsEdgePoint(request.Edit, coordinates))
            {
                //This may be an edge point

                //in the case of an insert, the supplied index may be outside the existing geometry, so 
                //we cannot use that to find connecting points
                var exsistingIndex = request.Edit.Operation == EditOperation.Insert && index > coordinates.Length - 1 
                    ? index - 1 
                    : index;

                var isSingleLineForPoint = IsSingleLineForPoint(request);
                //if the line is a single line making up a polygon, we have to consider the line itself for connecting points
                var connects = isSingleLineForPoint
                    ? GetConnectingPoints(new List<NgisFeature>() { request.Feature }, originalGeometry, exsistingIndex) 
                    : GetConnectingPoints(affectedFeatures, originalGeometry, exsistingIndex);

                if (connects.Any())
                {
                    foreach (var connect in connects) { 
                        //remove the connected feature
                        modifiedFeatures = modifiedFeatures
                            .Where(f => NgisFeatureHelper.GetLokalId(f) != NgisFeatureHelper.GetLokalId(connect.Feature))
                            .ToList();
                       
                        if (request.Edit.Operation == EditOperation.Delete)
                        {
                            var edited = originalGeometry.Coordinates[index == 0 ? 1 : coordinates.Length - 2];
                            var newPos = new List<double>(){ edited.X,edited.Y };
                            modifiedFeatures.Add(ApplyChange(connect.Feature, new EditLineOperation()
                            {
                                NodeValue = newPos,
                                Operation = EditOperation.Edit,
                                NodeIndex = connect.Index
                            }));
                        }
                        else
                        {
                            modifiedFeatures.Add(ApplyChange(connect.Feature, new EditLineOperation()
                            {
                                NodeValue = request.Edit.NodeValue,
                                Operation = EditOperation.Edit,
                                NodeIndex = connect.Index
                            }));
                        }
                    }
                }
            }
            
            return modifiedFeatures.ToList();
        }

        private static bool IsSingleLineForPoint(EditLineRequest request)
        {
            if (request.AffectedFeatures?.Count == 1 && request.AffectedFeatures[0].Geometry.GeometryType == "Polygon")
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
                _ => lineFeature.Geometry,

            };
            NgisFeatureHelper.SetOperation(lineFeature, Operation.Replace);
            return lineFeature;

        }

        private static List<ConnectingPoint> GetConnectingPoints(List<NgisFeature> affectedFeatures, LineString line, int index)
        {
            var coordinate = line.Coordinates[index];
            var buffered = new Point(coordinate).Buffer(line.Length * 0.001);
            var candidates = affectedFeatures.Where(f => f.Geometry.GeometryType == "LineString").Where(f => f.Geometry.Intersects(buffered));

            var connectingPoints = new List<ConnectingPoint>();
            
            foreach (var candidate in candidates)
            {
                if (candidate.Geometry.Coordinates.First().Equals2D(coordinate))
                {
                    connectingPoints.Add(new ConnectingPoint() { Index = 0, Feature = candidate });
                }
                if (candidate.Geometry.Coordinates.Last().Equals2D(coordinate))
                {
                    connectingPoints.Add(new ConnectingPoint() { Index = candidate.Geometry.Coordinates.Length - 1, Feature = candidate });
                }
            }
            return connectingPoints;
        }

        private static LineString ReplaceNode(LineString line, int index, Coordinate? newValue)
        {
            if (newValue == null)
            {
                throw new BadRequestException("Missing Coordinate value");
            }

            var currentCoordinate = line[index].CoordinateValue;
            currentCoordinate.CoordinateValue = newValue;
            return line;
        }

        private static Geometry InsertPoint(Geometry geom, int index, Coordinate? newPoint)
        {
            if (newPoint == null)
            {
                throw new BadRequestException("Missing Coordinate value");
            }

            var element = (LineString)geom;
            var oldSeq = element.CoordinateSequence;
            var newSeq = element.Factory.CoordinateSequenceFactory.Create(oldSeq.Count + 1, oldSeq.Dimension, oldSeq.Measures);

            if (index == 0) // Before first point
            {                
                newSeq.SetCoordinate(0, newPoint);
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 1, oldSeq.Count);
            }
            else if (index == oldSeq.Count) // Last point
            {                 
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, oldSeq.Count);
                newSeq.SetCoordinate(oldSeq.Count, newPoint);
            }
            else //point in the middle
            {
                CoordinateSequences.Copy(oldSeq, 0, newSeq, 0, index );
                newSeq.SetCoordinate(index , newPoint);
                CoordinateSequences.Copy(oldSeq, index , newSeq, index +1 , newSeq.Count - 1 - index);
            }
            
            return geom.Factory.CreateLineString(newSeq);            
        }

        private static Geometry DeletePoint(Geometry geom, int index)
        {
            var element = (LineString)geom;
            if (element.Count < 3)
            {
                throw new InvalidOperationException("LineString must contain at least 3 points");
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

        private static NgisFeature? RecreatePolygon(NgisFeature polygonFeature, IEnumerable<NgisFeature> references)
        {
            var created = _polygonCreator.CreatePolygonFromLines(references.ToList(), null).FirstOrDefault();

            if (created == null || !created.IsValid)
            {
                return null;
            }

            polygonFeature.Geometry = created.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon")?.Geometry!;
            NgisFeatureHelper.SetOperation(polygonFeature, Operation.Replace);
            return polygonFeature;
        }

        private static IEnumerable<NgisFeature> GetReferencedFeatures(NgisFeature feature, IEnumerable<NgisFeature> candidates)
            => NgisFeatureHelper.GetAllReferences(feature)
                .Select(lokalId => candidates.FirstOrDefault(f => NgisFeatureHelper.GetLokalId(f) == lokalId))
                .OfType<NgisFeature>();

        private static TopologyResponse EmptyInvalidResponse() => new() { IsValid = false, AffectedFeatures = new List<NgisFeature>() };
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
