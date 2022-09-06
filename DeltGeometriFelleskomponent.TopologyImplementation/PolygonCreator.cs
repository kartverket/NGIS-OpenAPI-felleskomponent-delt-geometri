using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;
using System.Linq;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class PolygonCreator
{
    public TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request)
    {
        request.Feature.Geometry = EnsureOrdering((Polygon)request.Feature.Geometry);
        NgisFeatureHelper.EnsureLocalId(request.Feature);
        var exteriorLine = CreateExteriorLineForPolyon(request.Feature);
        var interiorLines = CreateInteriorLinesForPolyon(request.Feature);
        return new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures.Concat(new List<NgisFeature>() { request.Feature, exteriorLine }).Concat(interiorLines).ToList(),
            IsValid = true
        };
    }

    public TopologyResponse CreatePolygonFromLines(List<NgisFeature> lineFeatures, Point? centroid)
    {
        // Now supports  multiple linestrings and order
        var polygonizer = new Polygonizer(extractOnlyPolygonal: true);
        polygonizer.Add(lineFeatures.Select(lf => lf.Geometry).ToList());

        if (polygonizer.GetPolygons().Count == 0)
        {
            return new TopologyResponse()
            {
                IsValid = false
            };
        }

        var polygon = (Polygon)polygonizer.GetPolygons().First();
        var isValid = polygon.IsValid;

        polygon = EnsureOrdering(polygon);

        if (centroid != null)
        {
            var inside = polygon.Contains(centroid);
            Console.WriteLine("Point is inside polygon:{0}", inside);
            isValid = inside;
        }


        var cutEdges = polygonizer.GetCutEdges();
        var dangels = polygonizer.GetDangles();

        if (cutEdges.Count > 0) Console.WriteLine("cutEdges.Count:{0}", cutEdges.Count);
        if (dangels.Count > 0) Console.WriteLine("dangels.Count:{0}", dangels.Count);


        var lokalId = Guid.NewGuid().ToString();


        var polygonFeature = NgisFeatureHelper.CreateFeature(polygon, lokalId);


        IEnumerable<NgisFeature>? referencesExterior = null;
        IEnumerable<List<NgisFeature>>? referencesInteriors = null;


        if (polygon != null)
        {
            var exterior = polygon.ExteriorRing;
            var interiors = polygon.InteriorRings;
            referencesExterior = lineFeatures.Where(feature => feature.Geometry.CoveredBy(exterior));
            referencesInteriors = interiors.Select(hole =>
                lineFeatures.Where(feature => feature.Geometry.CoveredBy(hole)).ToList()).Where(hole => hole.Count > 0);

            NgisFeatureHelper.SetReferences(polygonFeature, referencesExterior, referencesInteriors);
        }

        else
        {
            NgisFeatureHelper.SetReferences(polygonFeature, lineFeatures, null);
        }
        NgisFeatureHelper.SetOperation(polygonFeature, Operation.Create);

        lineFeatures.ForEach(a => NgisFeatureHelper.SetReferences(a, new List<string>() { lokalId }, null));

        //CreatePolygonFromLines now return NgisFeature FeatureReferences for lines in addition to the new polygon
        var affectedExteriors = referencesExterior.ToList().ToList();
        var affectedInteriors = referencesInteriors.SelectMany(listNgisInterior => listNgisInterior).ToList();
        var affectedPolygon = new List<NgisFeature>() { polygonFeature };
        var affected = affectedExteriors.Concat(affectedInteriors).Concat(affectedPolygon);
        
        return new TopologyResponse()
        {
            AffectedFeatures = affected.ToList(),
            IsValid = isValid
        };

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

    private static Polygon EnsureOrdering(Polygon polygon)
    {
        var ring = polygon.Shell.IsCCW ? polygon.Shell : (LinearRing)polygon.Shell.Reverse();
        var holes = polygon.Holes.Select(hole => hole.IsCCW ? (LinearRing)hole.Reverse() : hole).ToArray();
        return new Polygon(ring, holes);
    }
}