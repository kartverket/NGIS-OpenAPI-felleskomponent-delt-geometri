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
        var holeEdits = PolygonHoleEditor.GetHoleEdits(request);

        var editCount = shellEdits.Count() + holeEdits.Count();

        if (editCount == 0)
        {
            throw new Exception("No edits found");
        }
        if (editCount > 1)
        {
            throw new Exception("Multiple edits found. Not supported!");
        }

        return shellEdits.Count() > 0
            ? LineEditor.EditLine(shellEdits.First())
            : PolygonHoleEditor.EditHole(holeEdits.First());        
    }

    public static IEnumerable<EditLineRequest> CreateEditLineRequests(IEnumerable<Change> changes, IEnumerable<NgisFeature> referencedFeatures, NgisFeature editedPolygonFeature, LinearRing ring)
        => changes.Select(change =>
        {
            var referencedLineFeature = GetLineFeatureAffectedByChange(change, referencedFeatures, ring);
            if (referencedLineFeature == null)
            {
                return null;
            }

            var edit = CreateEditLineRequest(change, referencedLineFeature);
            if (edit == null)
            {
                return null;
            }

            edit.AffectedFeatures = new List<NgisFeature>() { editedPolygonFeature }.Concat(referencedFeatures.Where(f => NgisFeatureHelper.GetLokalId(referencedLineFeature) != NgisFeatureHelper.GetLokalId(f))).ToList();
            return edit;
        }).OfType<EditLineRequest>();

    public static IEnumerable<NgisFeature> GetFeaturesReferencedByPolygon(NgisFeature feature, List<NgisFeature>? affectedFeatures)
    {
        if (affectedFeatures == null)
        {
            return new List<NgisFeature>();
        }

        var directReferences = NgisFeatureHelper.GetAllReferences(feature);

        var references = affectedFeatures
            .Where(f => f.Geometry.GeometryType == "Polygon")
            .Where(p => NgisFeatureHelper.GetAllReferences(p).Any(r => directReferences.Contains(r)))
            .Select(p => new List<string>() { NgisFeatureHelper.GetLokalId(p) }.Concat(NgisFeatureHelper.GetAllReferences(p)))
            .SelectMany(p => p)
            .Concat(directReferences)
            .Distinct();
        return affectedFeatures.FindAll(f => references.Any(id => id == NgisFeatureHelper.GetLokalId(f)));
    }

    public static IEnumerable<Change> GetChangesToRing(LinearRing oldRing, LinearRing newRing)
    {
        var deletedVertices = CoordinateHelper.GetCoordinatesNotIn(oldRing, newRing);
        var newVertices = CoordinateHelper.GetCoordinatesNotIn(newRing, oldRing);

        var changes = new List<Change>();

        foreach (var deletedVertex in deletedVertices)
        {
            var newVertex = CoordinateHelper.GetClosestCoordinate(newVertices, deletedVertex);
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
                NewVertexPrevIndex = CoordinateHelper.FindCoordinateIndex(newRing.Coordinates, addedVertex) - 1
            });
        }
        return changes;
    }

    private static IEnumerable<EditLineRequest> GetShellEdits(EditPolygonRequest request)
    {
        var oldPolygon = (Polygon)request.Feature.Geometry;
        var newPolygon = request.EditedGeometry;
        var changes = GetChangesToRing(oldPolygon.Shell, newPolygon.Shell);
        return CreateEditLineRequests(changes, GetFeaturesReferencedByPolygon(request.Feature, request.AffectedFeatures), request.Feature, oldPolygon.Shell).OfType<EditLineRequest>();
    }

    private static NgisFeature? GetLineFeatureAffectedByChange(Change change, IEnumerable<NgisFeature> referencedFeatures, LinearRing ring)
    {        
        if (change.OldVertex != null)
        {
            return CoordinateHelper.GetFirstFeatureWithCoordinate(change.OldVertex, referencedFeatures);
        }
        else if (change.NewVertexPrevIndex.HasValue)
        {
            var coord = change.NewVertexPrevIndex.Value != -1 ? ring.Coordinates[change.NewVertexPrevIndex.Value] : ring.Coordinates.Last();
            return CoordinateHelper.GetFirstFeatureWithCoordinate(coord, referencedFeatures);
        }
        return null;
    }

    private static EditLineRequest? CreateEditLineRequest(Change change, NgisFeature feature)
    {
        if (change.NewVertex != null && change.OldVertex != null) { 

            return new EditLineRequest()
            {
                Feature = feature,
                Edit = new EditLineOperation()
                {
                    Operation = EditOperation.Edit,
                    NodeValue = new List<double>() { change.NewVertex.X, change.NewVertex.Y },
                    NodeIndex = CoordinateHelper.FindCoordinateIndex(feature.Geometry.Coordinates, change.OldVertex)
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
                    NodeIndex = CoordinateHelper.FindCoordinateIndex(feature.Geometry.Coordinates,change.OldVertex)
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



    public class Change {
        public Coordinate? OldVertex { get; set; }
        public Coordinate? NewVertex { get; set; }
        public int? NewVertexPrevIndex { get; set; }
    }

    public class HoleEditRequest
    {
        public int? Index { get; set; }
        public EditOperation Operation { get; set; }
        public LinearRing Ring { get; set; }
        public NgisFeature Feature { get; set; }
        public List<NgisFeature>? AffectedFeatures { get; set; }
    }

}


