using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using System.Net.NetworkInformation;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class PolygonEditor
{
    public static TopologyResponse EditPolygon(EditPolygonRequest request)
    {

        if (request.Feature.Geometry.GeometryType != "Polygon")
        {
            throw new Exception("Can only edit polygons");
        }

        var edits = GetShellEdits(request);

        if (edits.Count() == 0)
        {
            throw new Exception("No edits found");
        }
        if (edits.Count() > 1)
        {
            throw new Exception("Multiple edits found. Not supported!");
        }
        return LineEditor.EditLine(edits.First());        
    }

    private static IEnumerable<EditLineRequest> GetShellEdits(EditPolygonRequest request)
    {
        var oldPolygon = (Polygon)request.Feature.Geometry;
        var newPolygon = request.EditedGeometry; ;

        var pairs = GetPairs(oldPolygon.Shell, newPolygon.Shell);

        return ToEdits(pairs, GetShellFeatures(request.Feature, request.AffectedFeatures), request.Feature, oldPolygon.Shell).OfType<EditLineRequest>();
    }

    private static IEnumerable<EditLineRequest> ToEdits(IEnumerable<Pair> pairs, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature, LinearRing ring)
    {
        var res = new List<EditLineRequest>();
        foreach (var pair in pairs)
        {

            var referencedLineFeature = GetLineForEditPair(pair, referencedFeatures, ring);
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

    private static NgisFeature? GetLineForEditPair(Pair pair, IEnumerable<NgisFeature> referencedFeatures, LinearRing ring)
    {        
        if (pair.OldCoord != null)
        {
            return GetFirstFeatureWithCoordinate(pair.OldCoord, referencedFeatures);
        }
        else if (pair.NewCoordPrevIndex.HasValue)
        {
            var coord = pair.NewCoordPrevIndex.Value != -1 ? ring.Coordinates[pair.NewCoordPrevIndex.Value] : ring.Coordinates.Last();
            return GetFirstFeatureWithCoordinate(coord, referencedFeatures);
        }
        return null;
    }

    private static NgisFeature? GetFirstFeatureWithCoordinate(Coordinate coord, IEnumerable<NgisFeature> referencedFeatures)
        => referencedFeatures.FirstOrDefault(f => f.Geometry.Coordinates.Any(c2 => c2.Equals(coord)));

    private static IEnumerable<NgisFeature> GetShellFeatures(NgisFeature feature, List<NgisFeature>? affectedFeatures)
    {
        var exteriors = NgisFeatureHelper.GetExteriors(feature).Select(NgisFeatureHelper.RemoveSign);
        return affectedFeatures != null ? affectedFeatures.FindAll(f => exteriors.Any(id => id == NgisFeatureHelper.GetLokalId(f))) : new List<NgisFeature>();
    }

    private static EditLineRequest? ToEdit(Pair pair, NgisFeature feature)
    {
        if (pair.NewCoord != null && pair.OldCoord != null) { 

            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeValue = new List<double>() { pair.NewCoord.X, pair.NewCoord.Y },
                    NodeIndex = FindIndex(feature.Geometry.Coordinates, pair.OldCoord)
                }
            };
        }
        if (pair.NewCoord == null && pair.OldCoord != null)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Delete,                    
                    NodeIndex = FindIndex(feature.Geometry.Coordinates,pair.OldCoord)
                }
            };
        }
        if (pair.NewCoord != null && pair.OldCoord == null && pair.NewCoordPrevIndex.HasValue)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Insert,
                    NodeValue = new List<double>() { pair.NewCoord.X, pair.NewCoord.Y },
                    NodeIndex = pair.NewCoordPrevIndex.Value
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
                OldCoord = deletedPoint,
                NewCoord = newCoord,
            });
            newPoints = newPoints.Where(c => !c.Equals(newCoord));
        }
        foreach (var addedPoint in newPoints)
        {
            pairs.Add(new Pair()
            {
                OldCoord = null,
                NewCoord = addedPoint,
                NewCoordPrevIndex = FindIndex(newRing.Coordinates, addedPoint) - 1
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
        public Coordinate? OldCoord { get; set; }
        public Coordinate? NewCoord { get; set; }
        public int? NewCoordPrevIndex { get; set; }
    }
    
}


