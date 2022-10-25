using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using static DeltGeometriFelleskomponent.TopologyImplementation.PolygonEditor;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class PolygonHoleEditor
{
    public static TopologyResponse EditHole(HoleEditRequest edit)
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
            var edits = CreateEditLineRequests(GetChangesToRing(oldRing, newRing), GetFeaturesReferencedByPolygon(feature, edit.AffectedFeatures), feature, oldRing);
            if (edits.Count() > 1)
            {
                throw new Exception("Multiple edits found. Not supported!");
            }
            return LineEditor.EditLine(edits.First());

        }
        Func<HoleEditRequest, List<NgisFeature>> func = edit.Operation == EditOperation.Insert ? AddHole : RemoveHole;
        return new TopologyResponse()
        {
            IsValid = true,
            AffectedFeatures = func(edit).Concat(edit.AffectedFeatures != null ? edit.AffectedFeatures : new List<NgisFeature>()).ToList()
        };
    }

    public static IEnumerable<HoleEditRequest> GetHoleEdits(EditPolygonRequest request)
    {
        var oldHoles = GetHoles((Polygon)request.Feature.Geometry);
        var newHoles = GetHoles(request.EditedGeometry);

        if (oldHoles.Count() == newHoles.Count())
        {
            var changedHoles = newHoles.Where(h => !oldHoles.Any(oh => oh.Equals(h)));
            var edits = new List<HoleEditRequest>();
            var holes = GetHoles((Polygon)request.Feature.Geometry);
            foreach (var changedHole in changedHoles)
            {
                var oldHole = GetMostSimilarHole(oldHoles, changedHole);
                oldHoles = oldHoles.Where(h => !h.Equals(oldHole)).ToArray();

                edits.Add(new HoleEditRequest()
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
            return CoordinateHelper.GetRingsNotIn(oldHoles, newHoles).Select(hole => new HoleEditRequest()
            {
                Operation = EditOperation.Insert,
                Ring = hole,
                Feature = request.Feature,
                AffectedFeatures = request.AffectedFeatures
            }).ToList();

        }
        if (oldHoles.Count() > newHoles.Count())
        {
            return CoordinateHelper.GetRingsNotIn(newHoles, oldHoles).Select(hole => new HoleEditRequest()
            {
                Operation = EditOperation.Delete,
                Ring = hole,
                Feature = request.Feature,
                AffectedFeatures = request.AffectedFeatures
            }).ToList();
        }

        return new List<HoleEditRequest>();
    }

    private static LinearRing[] GetHoles(Polygon polygon)
        => polygon.Holes ?? new LinearRing[] { };

    private static LinearRing GetMostSimilarHole(LinearRing[] candidates, LinearRing hole)
    {
        return candidates[0];
    }

    private static List<NgisFeature> AddHole(HoleEditRequest edit)
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

        return new List<NgisFeature>() { feature, hole };
    }

    private static List<NgisFeature> RemoveHole(HoleEditRequest edit)
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

}
