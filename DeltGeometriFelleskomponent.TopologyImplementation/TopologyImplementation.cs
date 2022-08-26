using System.Diagnostics;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation : ITopologyImplementation
{
    public TopologyResponse ResolveReferences(ToplogyRequest request)
        => request.Feature.Update?.Action switch
        {
            Operation.Create => HandleCreate(request),
            Operation.Erase => HandleDelete(request),
            Operation.Replace => HandleUpdate(request),
            null => throw new ArgumentException("")
        };

    private TopologyResponse HandleCreate(ToplogyRequest request)
        => request.Feature.Geometry switch
        {
            Polygon => HandlePolygon(request),
            Geometry => new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() { request.Feature },
                IsValid = true
            },
            null => new TopologyResponse()

        };

    private TopologyResponse HandlePolygon(ToplogyRequest request)
    {
        var result = new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures
        };

        if (request.Feature.Geometry.IsEmpty)
        {

            // Polygonet er tomt, altså ønsker brukeren å lage et nytt polygon basert på grenselinjer

            if (request.Feature.Geometry_Properties.Exterior == null) return new TopologyResponse();
            if (request.AffectedFeatures == null) return new TopologyResponse();

            var affected = request.AffectedFeatures.ToDictionary(NgisFeatureHelper.GetLokalId, a => a);

            var referredFeatures = new List<NgisFeature>();
            foreach (var referred in request.Feature.Geometry_Properties.Exterior)
            {
                if (affected.TryGetValue(referred, out var referredFeature))
                {
                    referredFeatures.Add(referredFeature);
                }
                else
                {
                    throw new Exception("Referred feature not present in AffectedFeatures");
                }
            }

            // If interiors as input. Note that if interiors is not specified, we'll find the holes either way
            if (request.Feature.Geometry_Properties.Interiors != null && request.Feature.Geometry_Properties.Interiors.Count > 0)
            {
                foreach (var hole in request.Feature.Geometry_Properties.Interiors)
                {

                    foreach (var referred in hole)
                    {
                        if (affected.TryGetValue(referred, out var referredFeature))
                        {
                            referredFeatures.Add(referredFeature);
                        }
                        else
                        {
                            throw new Exception("Referred feature not present in AffectedFeatures");
                        }
                    }
                }
            }


            //request.Feature.Centroid
            var resultPolygon = CreatePolygonFromLines(request.Feature.Type, referredFeatures, null, out var isValid);

            result.AffectedFeatures.Add(resultPolygon);
            result.IsValid = isValid; // true;

        }
        else
        {
            var resultLine = CreateLineFromPolyon($"{request.Feature.Type}Grense", request.Feature);

            result.AffectedFeatures.Add(request.Feature);
            result.AffectedFeatures.Add(resultLine);
            result.IsValid = true;
        }

        return result;
    }

    private TopologyResponse HandleDelete(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private TopologyResponse HandleUpdate(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }


    private NgisFeature CreatePolygonFromLines(string type, List<NgisFeature> lineFeatures, Point? centroid, out bool isValid)
    {
        // use  Polygonizer polygonizer for all operations

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


                            referencesOnehole.Add( feature);
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
        return polygonFeature;
    }

    // TODO: Alternativ for å håndtere hull ?
    //public TopologyResponse CreatePolygonFromLines(List<NgisFeature> lineFeatures, Point? centroid)
    //{
    //    //if (a.Any((geom, id) => geom.))

    //    //1. hent ut geometrier
    //    //2. legg id på geometri (userData)
    //    //3. lag polygon

    //    var featureReferences = lineFeatures.Select(f => (f.Geometry, NgisFeatureHelper.GetLokalId(f)));

    //    foreach (var feat in lineFeatures)
    //    {
    //        //feat.Geometry
    //    }

    //    foreach (var feature in featureReferences)
    //    {
    //        //feature.Geometry.UserData = new 
    //    }

    //    //lage feature med polygonGEometry og exterior og interiors

    //    return null;
    //}

    private NgisFeature CreateLineFromPolyon(string type, NgisFeature polygonFeature)
    {
        var lokalId = Guid.NewGuid().ToString();

        var lineFeature = NgisFeatureHelper.CreateFeature(new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates), lokalId);

        NgisFeatureHelper.SetReferences(lineFeature, new List<string>() { lokalId }, null);
        NgisFeatureHelper.SetOperation(lineFeature, Operation.Create);

        NgisFeatureHelper.SetReferences(polygonFeature, new List<string>() { lokalId }, new List<IEnumerable<string>>());

        //set type to type
        return lineFeature;
    }

    //private IList<IPoint> Contains(IGeometry geom, IEnumerable<IPoint> points)
    //{
    //    var prepGeom = new NetTopologySuite.Geometries.Prepared.PreparedGeometryFactory().Prepare(geom);
    //    var res = new List<IPoint>();
    //    foreach (var point in points)
    //    {
    //        if (prepGeom.Contains(point)) res.Add(point);
    //    }
    //    return res;
    //}
}