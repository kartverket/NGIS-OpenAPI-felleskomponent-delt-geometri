using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;


namespace DeltGeometriFelleskomponent.TopologyImplementation
{
    public class GeometryEdit
    {
        public static NgisFeature? EditObject(EditLineRequest request)
        {
            var affectedFeatures = request.AffectedFeatures;
            var editOperation = request.Edit.Operation;
            var lineFeature = request.Feature;
            var index = request.Edit.NodeIndex;
            var newCoordinate = request.Edit.NodeCoordinate;
            // TODO: Implement more advanced editing when we edit on a "node" or a shared geometry line

            var existingGeometry = (LineString)lineFeature.Geometry.Copy();

            lineFeature.Geometry = editOperation switch
            {
                EditOperation.Edit => ReplaceNode(existingGeometry, index, newCoordinate),
                EditOperation.Delete => DeletePoint(existingGeometry, index),
                EditOperation.Insert => InsertPoint(existingGeometry, index, newCoordinate),
            };

            return NgisFeatureHelper.SetOperation2(lineFeature, Operation.Replace);
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

            var useOld = false;
            var element = (LineString)geom;

            var oldSeq = element.CoordinateSequence;
            var newSeq = element.Factory.CoordinateSequenceFactory.Create(
                oldSeq.Count + 1, oldSeq.Dimension, oldSeq.Measures);

            if (useOld) {
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
            }
            else
            {
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
