using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class PolygonEditor
{
    public static TopologyResponse EditPolygon(EditPolygonRequest request)
    {

        if (request.Feature.Geometry.GeometryType != "Polygon")
        {
            throw new Exception("Can only edit polygons");
        }
        var oldPolygon = (Polygon)request.Feature.Geometry;
        var newPolygon = request.EditedGeometry; ;

        var pairs = GetPairs(oldPolygon.Shell, newPolygon.Shell);
        var lines = GetShellFeatures(request.Feature, request.AffectedFeatures);
        var edits = ToEdits(pairs, lines, request.Feature).OfType<EditLineRequest>();


        if (edits.Count() == 1)
        {
            return LineEditor.EditLine(edits.First());
        }


        throw new NotImplementedException();
    }


    private static IEnumerable<EditLineRequest> ToEdits(IEnumerable<Pair> pairs, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature)
    {
        var res = new List<EditLineRequest>();
        foreach (var pair in pairs)
        {
            NgisFeature? referencedLineFeature = null;
            if (pair.oldCoord != null) {
                referencedLineFeature = GetFirstFeatureWithCoordinate(pair.oldCoord, referencedFeatures);
            } else if (pair.newCoordPrevIndex.HasValue)
            {
                var coord = pair.newCoordPrevIndex.Value != -1 ? ((Polygon)editedPolygonFeature.Geometry).Shell.Coordinates[pair.newCoordPrevIndex.Value] : ((Polygon)editedPolygonFeature.Geometry).Shell.Coordinates.Last();
                referencedLineFeature = GetFirstFeatureWithCoordinate(coord, referencedFeatures);
            }

            if (referencedLineFeature != null) { 
                var edit = ToEdit(pair, referencedLineFeature);
                if (edit != null) { 
                    edit.AffectedFeatures = new List<NgisFeature>() { editedPolygonFeature }.Concat( referencedFeatures.Where(f => NgisFeatureHelper.GetLokalId(f) != NgisFeatureHelper.GetLokalId(f))).ToList();
                    res.Add(edit);
                }
            }
        }
        return res;
    }

    private static NgisFeature? GetFirstFeatureWithCoordinate(Coordinate coord, IEnumerable<NgisFeature> referencedFeatures)
        => referencedFeatures.FirstOrDefault(f => f.Geometry.Coordinates.Any(c2 => c2.Equals(coord)));

    private static IEnumerable<NgisFeature> GetShellFeatures(NgisFeature feature, List<NgisFeature> affectedFeatures)
    {
        var exteriors = NgisFeatureHelper.GetExteriors(feature).Select(NgisFeatureHelper.RemoveSign);
        return affectedFeatures.FindAll(f => exteriors.Any(id => id == NgisFeatureHelper.GetLokalId(f)));
    }

    private static EditLineRequest? ToEdit(Pair pair, NgisFeature feature)
    {
        if (pair.newCoord != null && pair.oldCoord != null) { 

            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeValue = new List<double>() { pair.newCoord.X, pair.newCoord.Y },
                    NodeIndex = Array.FindIndex(feature.Geometry.Coordinates, c => c.Equals(pair.oldCoord))
                }
            };
        }
        if (pair.newCoord == null && pair.oldCoord != null)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Delete,                    
                    NodeIndex = Array.FindIndex(feature.Geometry.Coordinates, c => c.Equals(pair.oldCoord))
                }
            };
        }
        if (pair.newCoord != null && pair.oldCoord == null && pair.newCoordPrevIndex.HasValue)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Insert,
                    NodeValue = new List<double>() { pair.newCoord.X, pair.newCoord.Y },
                    NodeIndex = pair.newCoordPrevIndex.Value
                }
            };
        }
        return null;
    }

    private static IEnumerable<Coordinate> GetCoordsNotIn(LinearRing a, LinearRing b) 
        => a.Coordinates.Where(c => !b.Coordinates.Any(c2 => c.Equals(c2)));

    private static IEnumerable<Pair> GetPairs (LinearRing oldRing, LinearRing newRing)
    {
        var deletedPoints = GetCoordsNotIn(oldRing, newRing);
        var newPoints = GetCoordsNotIn(newRing, oldRing);

        var pairs = new List<Pair>();
        
        foreach (var deletedPoint in deletedPoints)
        {
            var newCoord = GetClosest(newPoints, deletedPoint);
            pairs.Add(new Pair()
            {
                oldCoord = deletedPoint,
                newCoord = newCoord,
            });
            newPoints = newPoints.Where(c => !c.Equals(newCoord));
        }
        foreach (var addedPoint in newPoints)
        {
            pairs.Add(new Pair()
            {
                oldCoord = null,
                newCoord = addedPoint,
                newCoordPrevIndex = FindIndex(newRing.Coordinates, addedPoint) - 1
            });
        }

        return pairs;
    }

    private static int FindIndex(Coordinate[] coordinates, Coordinate coord)
        => Array.FindIndex(coordinates, c => c.Equals(coord));

    private static Coordinate? GetClosest (IEnumerable<Coordinate> points, Coordinate point)
    {
        var res = points.Select(p => (p.Distance(point), p));
        return res.Count() > 0 ? res.Min().Item2 : null;

    }

    internal class Pair {
        public Coordinate? oldCoord { get; set; }
        public Coordinate? newCoord { get; set; }
        public int? newCoordPrevIndex { get; set; }
    }

    
    
}


