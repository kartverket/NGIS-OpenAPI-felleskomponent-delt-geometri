using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class PolygonCreator
{
    public TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request)
    {
        NgisFeatureHelper.EnsureLocalId(request.Feature);
        var exteriorLine = CreateExteriorLineForPolyon( request.Feature);
        var interiorLines = CreateInteriorLinesForPolyon(request.Feature);
        return new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures.Concat(new List<NgisFeature>(){request.Feature, exteriorLine}).Concat(interiorLines).ToList(),
            IsValid = true
        };
    }

    public TopologyResponse CreatePolygonFromLines(List<NgisFeature> lineFeatures, Point? centroid)
    {
        // use  Polygonizer polygonizer for all operations
        var isValid = true;
        Polygon? polygon = null;
        // Now supports  multiple linestrings and order
        Polygonizer polygonizer = new Polygonizer(extractOnlyPolygonal: true);

        foreach (var lineFeature in lineFeatures)
        {
            polygonizer.Add(lineFeature.Geometry);
        }

        if (polygonizer.GetPolygons().Count > 0)
        {
            polygon = (Polygon)polygonizer.GetPolygons().First();
            isValid = polygon.IsValid;

            if (!polygon.Shell.IsCCW)
            {
                // TODO: Check if polygon.Reverse is enough
                Console.WriteLine("Polygon is not CCW");
                polygon = (Polygon)polygon.Reverse(); // reverse polygon
            }

            if (centroid != null)
            {
                var inside = polygon.Contains(centroid);
                Console.WriteLine("Point is inside polygon:{0}", inside);
                isValid = inside;
            }
        }
        else
        {
            isValid = false;
        }

        var cutEdges = polygonizer.GetCutEdges();
        var dangels = polygonizer.GetDangles();

        if (cutEdges.Count > 0) Console.WriteLine("cutEdges.Count:{0}", cutEdges.Count);
        if (dangels.Count > 0) Console.WriteLine("dangels.Count:{0}", dangels.Count);


        var lokalId = Guid.NewGuid().ToString();


        var polygonFeature = NgisFeatureHelper.CreateFeature(polygon, lokalId);

        if (polygon != null)
        {


            var exterior = polygon.ExteriorRing;
            var interiors = polygon.InteriorRings;
            var referencesExterior = new List<NgisFeature>();
            List<List<NgisFeature>>? referencesInteriors = null;

            foreach (var feature in lineFeatures)
            {
                if (feature.Geometry.CoveredBy(exterior))
                {
                    referencesExterior.Add(feature);
                }
            }

            if (interiors != null && interiors.Length > 0)
            {
                foreach (var hole in interiors)
                {
                    List<NgisFeature>? referencesOnehole = null;
                    foreach (var feature in lineFeatures)
                    {
                        if (feature.Geometry.CoveredBy(hole))
                        {
                            //referencesInteriors ??= new List<List<NgisFeature>>();
                            if (referencesInteriors == null)
                            {
                                referencesInteriors = new List<List<NgisFeature>>();
                            }

                            referencesOnehole ??= new List<NgisFeature>();
                            //if (referencesOnehole == null)
                            //{
                            //    referencesOnehole = new List<NgisFeature>();
                            //}


                            referencesOnehole.Add(feature);
                        }
                    }

                    if (referencesOnehole != null) referencesInteriors?.Add(referencesOnehole);
                }
            }

            NgisFeatureHelper.SetReferences(polygonFeature, referencesExterior, referencesInteriors);
        }

        else
        {
            NgisFeatureHelper.SetReferences(polygonFeature, lineFeatures, null);
        }
        NgisFeatureHelper.SetOperation(polygonFeature, Operation.Create);

        lineFeatures.ForEach(a => NgisFeatureHelper.SetReferences(a, new List<string>() { lokalId }, null));

        //set type to type
        return new TopologyResponse()
        {
            AffectedFeatures = new List<NgisFeature>(){ polygonFeature },
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
        if (interiorFeatures.Count > 0) {
            //TODO: Handle winding?
            NgisFeatureHelper.SetInterior(polygonFeature, interiorFeatures.Select(f => new List<NgisFeature>(){f}));
        }
        return interiorFeatures;
    }

    private static NgisFeature CreateInteriorFeature(LineString ring)
    {
        var interiorFeature = NgisFeatureHelper.CreateFeature(ring);
        NgisFeatureHelper.SetOperation(interiorFeature, Operation.Create);
        return interiorFeature;
    }
}