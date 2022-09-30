using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;
using NetTopologySuite.Operation.Valid;
using System.Linq;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class PolygonCreator
{
    public TopologyResponse CreatePolygonFromGeometry(CreateGeometryRequest request)
    {
        request.Feature.Geometry = EnsureOrdering((Polygon)request.Feature.Geometry);
        request.Feature = NgisFeatureHelper.EnsureLocalId(request.Feature);
        var exteriorLine = CreateExteriorLineForPolyon(request.Feature);
        var interiorLines = CreateInteriorLinesForPolyon(request.Feature);
        return new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures.Concat(new List<NgisFeature>() { request.Feature, exteriorLine }).Concat(interiorLines).ToList(),
            IsValid = true
        };
    }

    public IEnumerable<TopologyResponse> CreatePolygonFromLines(List<NgisFeature> lineFeatures, List<Point>? centroids)
    {
        // Now supports  multiple linestrings and order
        var polygonizer = new Polygonizer();

        polygonizer.Add(lineFeatures.Select(lf => lf.Geometry).ToList());

        if (polygonizer.GetPolygons().Count == 0)
        {
            yield return new TopologyResponse()
            {
                IsValid = false
            };
        }

        var polygons = polygonizer.GetPolygons().ToList();

        var invalidRingLines = polygonizer.GetInvalidRingLines().ToList();

        foreach (Polygon polygon in polygons)
        {
            var isValid = polygon.IsValid;

            var orderedPolygon = EnsureOrdering(polygon);

            if (centroids != null)
            {
                var inside = centroids.Any(centroid => orderedPolygon.Contains(centroid));

                if (!inside) continue;

                Console.WriteLine("Point is inside polygon:{0}", inside);
                isValid = orderedPolygon.IsValid && inside;
            }


            var cutEdges = polygonizer.GetCutEdges();
            var dangels = polygonizer.GetDangles();

            if (cutEdges.Count > 0) Console.WriteLine("cutEdges.Count:{0}", cutEdges.Count);
            if (dangels.Count > 0) Console.WriteLine("dangels.Count:{0}", dangels.Count);


            var lokalId = Guid.NewGuid().ToString();


            var polygonFeature = NgisFeatureHelper.CreateFeature(orderedPolygon, lokalId);


            var referencesExterior = new List<NgisFeature>();
            var referencesInteriors = new List<List<NgisFeature>>();


            if (orderedPolygon != null)
            {
                var exteriorReferences = GetOrientedFeatures(orderedPolygon.ExteriorRing, lineFeatures).ToList();
                referencesExterior = exteriorReferences.Select(p => p.Feature).ToList();

                var interiorReferences = orderedPolygon.InteriorRings.Select(hole => GetOrientedFeatures(hole, lineFeatures).ToList()).ToList();
                referencesInteriors = interiorReferences.Select(hole => hole.Select(f => f.Feature).ToList()).ToList();

                NgisFeatureHelper.SetReferences(polygonFeature, exteriorReferences.Select(GetIdWithDirection), interiorReferences.Select(hole => hole.Select(GetIdWithDirection)));
            }

            else
            {
                NgisFeatureHelper.SetReferences(polygonFeature, lineFeatures, null);
            }
            NgisFeatureHelper.SetOperation(polygonFeature, Operation.Create);

            //CreatePolygonFromLines now return NgisFeature FeatureReferences for lines in addition to the new polygon
            var affectedExteriors = referencesExterior.ToList().ToList();
            var affectedInteriors = referencesInteriors.SelectMany(listNgisInterior => listNgisInterior).ToList();
            var affectedPolygon = new List<NgisFeature>() { polygonFeature };
            var affected = affectedExteriors.Concat(affectedInteriors).Concat(affectedPolygon);

            yield return new TopologyResponse()
            {
                AffectedFeatures = affected.ToList(),
                IsValid = isValid
            };
        }

    }

    private static NgisFeature CreateExteriorLineForPolyon(NgisFeature polygonFeature)
    {
        var exteriorFeature = NgisFeatureHelper.CreateFeature(new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates));
        NgisFeatureHelper.SetOperation(exteriorFeature, Operation.Create);
        //TODO: Handle winding?
        NgisFeatureHelper.SetReferences(polygonFeature, new List<NgisFeature>() { exteriorFeature }, null);
        return exteriorFeature;
    }

    private static IEnumerable<NgisFeature> CreateInteriorLinesForPolyon(NgisFeature polygonFeature)
    {
        var interiorFeatures = ((Polygon)polygonFeature.Geometry).InteriorRings.Select(CreateInteriorFeature).ToList();
        if (interiorFeatures.Count > 0)
        {
            //TODO: Handle winding?
            NgisFeatureHelper.SetInterior(polygonFeature, interiorFeatures.Select(f => new List<NgisFeature>() { f }));
        }
        return interiorFeatures;
    }

    private static NgisFeature CreateInteriorFeature(LineString ring)
    {
        var interiorFeature = NgisFeatureHelper.CreateFeature(ring);
        NgisFeatureHelper.SetOperation(interiorFeature, Operation.Create);
        return interiorFeature;
    }

    public static Polygon EnsureOrdering(Polygon polygon)
    {
        var ring = polygon.Shell.IsCCW ? polygon.Shell : (LinearRing)polygon.Shell.Reverse();
        var holes = polygon.Holes.Select(hole => hole.IsCCW ? (LinearRing)hole.Reverse() : hole).ToArray();
        return new Polygon(ring, holes);
    }

    private static bool IsSubList(IReadOnlyList<Coordinate> list, IReadOnlyList<Coordinate> sublist)
    {
        for (var i = 1; i < list.Count(); i++)
        {
            var a = list[i - 1];
            var b = list[i];
            if (a.Equals(sublist[0]) && b.Equals(sublist[1]))
            {
                return true;
            }

        }

        return false;
    }

    private static string GetIdWithDirection(FeatureWithDirection f) =>
        $"{(f.IsReversed ? "-" : "")}{NgisFeatureHelper.GetLokalId(f.Feature)}";


    private static IEnumerable<FeatureWithDirection> GetOrientedFeatures(Geometry ring, IEnumerable<NgisFeature> candidates)
        => candidates.Where(candidate => candidate.Geometry.CoveredBy(ring)).Select(candidate =>
        {
            var reversed = !IsSubList(ring.Coordinates, candidate.Geometry.Coordinates.Take(2).ToList());
            return new FeatureWithDirection(){Feature = candidate, IsReversed = reversed};
        });
    

    private class FeatureWithDirection 
    {
        public NgisFeature Feature { get; set; }
        public bool IsReversed { get; set; }
    }
}