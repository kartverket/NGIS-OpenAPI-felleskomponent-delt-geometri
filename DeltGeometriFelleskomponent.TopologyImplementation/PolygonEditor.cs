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
            if (!edit.Index.HasValue)
            {
                throw new Exception("Missing index");
            }
            
            var feature = edit.Feature;
            var polygon = (Polygon)feature.Geometry;

            var oldRing = polygon.Holes[edit.Index.Value];
            var newRing = edit.Ring;
            var edits = ToEdits(GetChanges(oldRing, newRing), GetFeaturesReferencedByPolygon(feature, edit.AffectedFeatures), feature, oldRing);
            if (edits.Count() > 1)
            {
                throw new Exception("Multiple edits found. Not supported!");
            }
            return LineEditor.EditLine(edits.First());
            
        }        
        Func<HoleEdit, List<NgisFeature>> func = edit.Operation == EditOperation.Insert ? AddHole : RemoveHole;
        return new TopologyResponse()
        {
            IsValid = true,
            AffectedFeatures = func(edit).Concat(edit.AffectedFeatures != null ? edit.AffectedFeatures : new List<NgisFeature>()).ToList()
        };
    }

    private static List<NgisFeature> AddHole(HoleEdit edit)
    {        
        var feature = edit.Feature;
        var polygon = (Polygon)feature.Geometry;

        var hole = PolygonCreator.CreateInteriorFeature(new LineString(edit.Ring.Coordinates));
        var ring = new LinearRing(hole.Geometry.Coordinates);

        var isReversed = ring.IsCCW;

        var holes = polygon.Holes.ToList();
        holes.Add(ring);        

        feature.Geometry = PolygonCreator.EnsureOrdering(new Polygon(polygon.Shell, holes.ToArray()));
        var interiors = NgisFeatureHelper.GetInteriors(feature);
        interiors.Add(new List<string>() { $"{(isReversed ? "-" : "")}{NgisFeatureHelper.GetLokalId(hole)}" });
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

        var changes = GetChanges(oldPolygon.Shell, newPolygon.Shell);

        return ToEdits(changes, GetFeaturesReferencedByPolygon(request.Feature, request.AffectedFeatures), request.Feature, oldPolygon.Shell).OfType<EditLineRequest>();
    }

    private static LinearRing[] GetHoles(Polygon polygon)
        => polygon.Holes ?? new LinearRing[] {};
    
    private static LinearRing GetMostSimilarHole(LinearRing[] candidates, LinearRing hole)
    {
        return candidates[0];
    }

    private static IEnumerable<HoleEdit> GetHoleEdits(EditPolygonRequest request)
    {
        var oldHoles = GetHoles((Polygon)request.Feature.Geometry);
        var newHoles = GetHoles(request.EditedGeometry);

        if (oldHoles.Count() == newHoles.Count())
        {
            var changedHoles = newHoles.Where(h => !oldHoles.Any(oh => oh.Equals(h)));
            var edits = new List<HoleEdit>();
            var holes = GetHoles((Polygon)request.Feature.Geometry);
            foreach (var changedHole in changedHoles)
            {
                var oldHole = GetMostSimilarHole(oldHoles, changedHole);
                oldHoles = oldHoles.Where(h => !h.Equals(oldHole)).ToArray();

                edits.Add(new HoleEdit()
                {
                    Operation = EditOperation.Edit,
                    Feature = request.Feature,
                    AffectedFeatures = request.AffectedFeatures,
                    Index = Array.FindIndex(holes, c => c.Equals(oldHole)),
                    Ring = changedHole
                });
            }
            return edits;

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

    private static IEnumerable<EditLineRequest> ToEdits(IEnumerable<Change> changes, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature, LinearRing ring)
    {
        var res = new List<EditLineRequest>();
        foreach (var change in changes)
        {

            var referencedLineFeature = GetLineForEditPair(change, referencedFeatures, ring);
            if (referencedLineFeature != null) { 
                var edit = ToEdit(change, referencedLineFeature);
                if (edit != null) { 
                    edit.AffectedFeatures = new List<NgisFeature>() { editedPolygonFeature }.Concat( referencedFeatures.Where(f => NgisFeatureHelper.GetLokalId(referencedLineFeature) != NgisFeatureHelper.GetLokalId(f))).ToList();
                    res.Add(edit);
                }
            }
        }
        return res;
    }

    private static NgisFeature? GetLineForEditPair(Change change, IEnumerable<NgisFeature> referencedFeatures, LinearRing ring)
    {        
        if (change.OldVertex != null)
        {
            return GetFirstFeatureWithCoordinate(change.OldVertex, referencedFeatures);
        }
        else if (change.NewVertexPrevIndex.HasValue)
        {
            var coord = change.NewVertexPrevIndex.Value != -1 ? ring.Coordinates[change.NewVertexPrevIndex.Value] : ring.Coordinates.Last();
            return GetFirstFeatureWithCoordinate(coord, referencedFeatures);
        }
        return null;
    }

    private static NgisFeature? GetFirstFeatureWithCoordinate(Coordinate coordinate, IEnumerable<NgisFeature> referencedFeatures)
        => referencedFeatures.FirstOrDefault(f => f.Geometry.Coordinates.Any(c2 => c2.Equals(coordinate)));

    private static IEnumerable<NgisFeature> GetFeaturesReferencedByPolygon(NgisFeature feature, List<NgisFeature>? affectedFeatures)
    {
        if (affectedFeatures == null)
        {
            return new List<NgisFeature>();
        }

        var directReferences = NgisFeatureHelper.GetAllReferences(feature);

        var references = affectedFeatures
            .Where(f => f.Geometry.GeometryType == "Polygon")
            .Where(p => NgisFeatureHelper.GetAllReferences(p).Any(r => directReferences.Contains(r)))
            .Select(p => new List<string>() {NgisFeatureHelper.GetLokalId(p) }.Concat(NgisFeatureHelper.GetAllReferences(p)))
            .SelectMany(p => p)
            .Concat(directReferences)
            .Distinct();
        return affectedFeatures.FindAll(f => references.Any(id => id == NgisFeatureHelper.GetLokalId(f)));
    }

    private static EditLineRequest? ToEdit(Change change, NgisFeature feature)
    {
        if (change.NewVertex != null && change.OldVertex != null) { 

            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeValue = new List<double>() { change.NewVertex.X, change.NewVertex.Y },
                    NodeIndex = FindIndex(feature.Geometry.Coordinates, change.OldVertex)
                }
            };
        }
        if (change.NewVertex == null && change.OldVertex != null)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Delete,                    
                    NodeIndex = FindIndex(feature.Geometry.Coordinates,change.OldVertex)
                }
            };
        }
        if (change.NewVertex != null && change.OldVertex == null && change.NewVertexPrevIndex.HasValue)
        {
            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Insert,
                    NodeValue = new List<double>() { change.NewVertex.X, change.NewVertex.Y },
                    NodeIndex = change.NewVertexPrevIndex.Value
                }
            };
        }
        return null;
    }

    private static IEnumerable<Coordinate> GetCoordsNotIn(LinearRing a, LinearRing b) 
        => a.Coordinates[..^1].Where(c => !b.Coordinates[..^1].Any(c2 => c.Equals(c2)));

    private static IEnumerable<Change> GetChanges (LinearRing oldRing, LinearRing newRing)
    {
        var deletedVertices = GetCoordsNotIn(oldRing, newRing);
        var newVertices = GetCoordsNotIn(newRing, oldRing);

        var changes = new List<Change>();
        
        foreach (var deletedVertex in deletedVertices)
        {
            var newVertex = GetClosest(newVertices, deletedVertex);
            changes.Add(new Change()
            {
                OldVertex = deletedVertex,
                NewVertex = newVertex,
            });
            newVertices = newVertices.Where(c => !c.Equals(newVertex));
        }
        foreach (var addedVertex in newVertices)
        {
            changes.Add(new Change()
            {
                OldVertex = null,
                NewVertex = addedVertex,
                NewVertexPrevIndex = FindIndex(newRing.Coordinates, addedVertex) - 1
            });
        }

        return changes;
    }

    private static int FindIndex(Coordinate[] coordinates, Coordinate coord)
        => Array.FindIndex(coordinates, c => c.Equals(coord));

    private static Coordinate? GetClosest (IEnumerable<Coordinate> cooordinates, Coordinate targetCoordinate)
    {
        var distances = cooordinates.Select(p => (p.Distance(targetCoordinate), p));
        return distances.Count() > 0 ? distances.Min().Item2 : null;
    }

    internal class Change {
        public Coordinate? OldVertex { get; set; }
        public Coordinate? NewVertex { get; set; }
        public int? NewVertexPrevIndex { get; set; }
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


