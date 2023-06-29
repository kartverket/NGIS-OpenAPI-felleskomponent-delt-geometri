using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.Models.Exceptions;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Polygonize;

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

                ReferenceHelper.SetReferences(polygonFeature, exteriorReferences, interiorReferences);
            }
            else
            {
                ReferenceHelper.SetReferences(polygonFeature, lineFeatures.Select(f => new FeatureWithDirection(){IsReversed = false, Feature = f}), null);
              
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
        ReferenceHelper.SetReferences(polygonFeature, new List<FeatureWithDirection>() {new ()
        {
            Feature = exteriorFeature, 
            IsReversed = false
        }}, null);

        return exteriorFeature;
    }

    private static IEnumerable<NgisFeature> CreateInteriorLinesForPolyon(NgisFeature polygonFeature)
    {
        var interiorFeatures = ((Polygon)polygonFeature.Geometry).InteriorRings.Select(CreateInteriorFeature).ToList();
        if (interiorFeatures.Count > 0)
        {
            //TODO: Handle winding?
           ReferenceHelper.AddInterior(polygonFeature, interiorFeatures.Select(f => new FeatureWithDirection(){Feature = f, IsReversed = false}));
        }
        return interiorFeatures;
    }

    public static NgisFeature CreateInteriorFeature(LineString ring)
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

    private static string GetIdWithDirection(FeatureWithDirection f) =>
        $"{(f.IsReversed ? "-" : "")}{NgisFeatureHelper.GetLokalId(f.Feature)}";


    private static IEnumerable<FeatureWithDirection> GetOrientedFeatures(Geometry ring, IEnumerable<NgisFeature> candidates)
    {
        //there is something wrong going on related to presicion
        //writing and reading wkt fixes this, but this is a hack, I know. But this should work for now...
        var w = new WKTWriter();
        var r = new WKTReader();
        var references = candidates.Where(candidate => r.Read(w.Write(candidate.Geometry)).CoveredBy(ring));
       
        var coords = ring.Coordinates;
        var first = references.FirstOrDefault(r => r.Geometry.Coordinates[0].Equals(coords[0]) && r.Geometry.Coordinates[1].Equals(coords[1]) );

        var index = 0;
        var res = new List<FeatureWithDirection>();
        while (references.Count() > 0) {
            var line = GetLineAtPosition(coords, index, references);
            res.Add(line);
            index += line.Feature.Geometry.Coordinates.Length -1;
            references = references.Where(r => NgisFeatureHelper.GetLokalId(r) != NgisFeatureHelper.GetLokalId(line.Feature));
        }        
        return res;        
    }
    
    private static FeatureWithDirection GetLineAtPosition(Coordinate[] coords, int index, IEnumerable<NgisFeature> references)
    {
        var rightWay = references.FirstOrDefault(r => StartsAtPosition(coords, index, r.Geometry));
        if (rightWay != null)
        {
            return new FeatureWithDirection { Feature = rightWay, IsReversed = false };
        }
        var reversed = references.FirstOrDefault(r => StartsAtPosition(coords, index, r.Geometry.Reverse()));
        if (reversed != null)
        {
            return new FeatureWithDirection { Feature = reversed, IsReversed = true };
        }
        throw new ExceptionWithHttpStatusCode("Unable to build polygon!", System.Net.HttpStatusCode.InternalServerError);
    }

    private static bool StartsAtPosition(Coordinate[] coords, int index, Geometry line)
        => line.Coordinates[0].Equals(coords[index]) && line.Coordinates[1].Equals(coords[index + 1]);
    

    public class FeatureWithDirection
    {
        public NgisFeature Feature { get; set; }
        public bool IsReversed { get; set; }
    }
}