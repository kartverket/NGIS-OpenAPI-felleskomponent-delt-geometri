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

        var shellEdits = GetShellEdits(request);
        var holeEdits = GetHoleEdits(request);

        var editCount = shellEdits.Count() + holeEdits.Count();

        if (editCount == 0)
        {
            throw new Exception("No edits found");
        }
        if (editCount > 1)
        {
            throw new Exception("Multiple edits found. Not supported!");
        }


        if (shellEdits.Count() > 0)
        {
            return LineEditor.EditLine(shellEdits.First());
        }
        return ApplyHoleEdit(holeEdits.First());

    }

    private static TopologyResponse ApplyHoleEdit(HoleEdit edit)
    {
        if (edit.Operation == EditOperation.Edit)
        {

        }
        else
        {
            Func<HoleEdit, List<NgisFeature>> func = edit.Operation == EditOperation.Insert ? AddHole : RemoveHole;
            return new TopologyResponse()
            {
                IsValid = true,
                AffectedFeatures = func(edit).Concat(edit.AffectedFeatures != null ? edit.AffectedFeatures : new List<NgisFeature>()).ToList()
            };

        }
       
        return new TopologyResponse() { IsValid = false, AffectedFeatures = new List<NgisFeature>() };
    }

    private static List<NgisFeature> AddHole(HoleEdit edit)
    {
        var hole = PolygonCreator.CreateInteriorFeature(new LineString(edit.Ring.Coordinates));

        var feature = edit.Feature;

        var polygon = (Polygon)feature.Geometry;        
        var holes = polygon.Holes.ToList();
        var ring = new LinearRing(hole.Geometry.Coordinates);

        var reverse = ring.IsCCW;
        

        holes.Add(ring);        
        feature.Geometry = PolygonCreator.EnsureOrdering(new Polygon(polygon.Shell, holes.ToArray()));
        var interiors = NgisFeatureHelper.GetInteriors(feature);
        interiors.Add(new List<string>() { $"{(reverse ? "-" : "")}{NgisFeatureHelper.GetLokalId(hole)}" });
        NgisFeatureHelper.SetInterior(feature, interiors);
        

        NgisFeatureHelper.SetOperation(feature, Operation.Replace);
        return new List<NgisFeature>() { feature, hole};
    }

    private static List<NgisFeature> RemoveHole(HoleEdit edit)
    {
        var feature = edit.Feature;
        var shell = ((Polygon)feature.Geometry).Shell;
        var holes = ((Polygon)feature.Geometry).Holes.Where(h => !h.Equals(edit.Ring)).ToArray();
        feature.Geometry = new Polygon(shell, holes);

        NgisFeatureHelper.SetOperation(feature, Operation.Replace);

        var ringFeature = edit.AffectedFeatures?.FirstOrDefault(f => f.Geometry.Equals(edit.Ring));
        var ringFeatureId = ringFeature != null ? NgisFeatureHelper.GetLokalId(ringFeature) : null;
        var interiors = NgisFeatureHelper.GetInteriors(feature);

        NgisFeatureHelper.SetInterior(feature, ringFeatureId != null ? interiors.Where(h => !h.Select(NgisFeatureHelper.RemoveSign).Contains(ringFeatureId)).ToArray() : interiors);
        return new List<NgisFeature>() { feature };
    }

    private static IEnumerable<EditLineRequest> GetShellEdits(EditPolygonRequest request)
    {
        var oldPolygon = (Polygon)request.Feature.Geometry;
        var newPolygon = request.EditedGeometry;

        var pairs = GetPairs(oldPolygon.Shell, newPolygon.Shell);

        return ToEdits(pairs, GetShellFeatures(request.Feature, request.AffectedFeatures), request.Feature, oldPolygon.Shell).OfType<EditLineRequest>();
    }

    private static LinearRing[] GetHoles(Polygon polygon)
        => polygon.Holes ?? new LinearRing[] {};
    

    private static IEnumerable<HoleEdit> GetHoleEdits(EditPolygonRequest request)
    {
        var oldHoles = GetHoles((Polygon)request.Feature.Geometry);
        var newHoles = GetHoles(request.EditedGeometry);

        if (oldHoles.Count() == newHoles.Count())
        {
            //check for edits
            return new List<HoleEdit>();
        }
        if (oldHoles.Count() < newHoles.Count())
        {
            return GetRingsNotIn(oldHoles, newHoles).Select(hole => new HoleEdit()
            {
                Operation = EditOperation.Insert,
                Ring = hole,
                Feature = request.Feature,
                AffectedFeatures = request.AffectedFeatures
            }).ToList();
           
        }
        if (oldHoles.Count() > newHoles.Count())
        {
            return GetRingsNotIn(newHoles,oldHoles).Select(hole => new HoleEdit()
            {
                Operation = EditOperation.Delete,
                Ring = hole,
                Feature = request.Feature,
                AffectedFeatures = request.AffectedFeatures
            }).ToList();
        }

        return new List<HoleEdit>();
    }

    private static LinearRing[] GetRingsNotIn(LinearRing[] a, LinearRing[] b)
        => b.Where(r => !a.Any(x => x.Equals(r))).ToArray();

    private static IEnumerable<EditLineRequest> ToEdits(IEnumerable<Pair> pairs, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature, LinearRing ring)
    {
        var res = new List<EditLineRequest>();
        foreach (var pair in pairs)
        {

            var referencedLineFeature = GetLineForEditPair(pair, referencedFeatures, ring);
            if (referencedLineFeature != null) { 
                var edit = ToEdit(pair, referencedLineFeature);
                if (edit != null) { 
                    edit.AffectedFeatures = new List<NgisFeature>() { editedPolygonFeature }.Concat( referencedFeatures.Where(f => NgisFeatureHelper.GetLokalId(referencedLineFeature) != NgisFeatureHelper.GetLokalId(f))).ToList();
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
        => a.Coordinates[..^1].Where(c => !b.Coordinates[..^1].Any(c2 => c.Equals(c2)));

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

    internal class HoleEdit
    {
        public int? Index { get; set; }
        public EditOperation Operation { get; set; }
        public LinearRing Ring { get; set; }
        public NgisFeature Feature { get; set; }
        public List<NgisFeature>? AffectedFeatures { get; set; }
    }

}


