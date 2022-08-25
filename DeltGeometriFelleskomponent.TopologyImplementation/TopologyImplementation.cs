using System.Diagnostics;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation : ITopologyImplementation
{
    private readonly PolygonCreator _polygonCreator = new();

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
            // Her er feilen med hull, mangler interiors
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

            return result;
        }
        return _polygonCreator.CreatePolygonFromGeometry(request);
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


        // Her er feil 2 med hull, mangler interiors, må bruke denne ?:
        // public static NgisFeature CreateFeature(Geometry geometry, string lokalId, Operation operation, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors)
        var polygonFeature = NgisFeatureHelper.CreateFeature(polygon, lokalId);
        NgisFeatureHelper.SetReferences(polygonFeature,lineFeatures, null);
        NgisFeatureHelper.SetOperation(polygonFeature, Operation.Create);

        lineFeatures.ForEach(a => NgisFeatureHelper.SetReferences(a, new List<string>(){lokalId}, null));

        //set type to type
        return polygonFeature;
    }

    // TODO: For å håndtere hull
    //public TopologyResponse CreatePolygonFromLines(List<NgisFeature> lineFeatures, Point? centroid)
    //{
    //    var a = lineFeatures.Select(f => (f.Geometry, NgisFeatureHelper.GetLokalId(f)));
    //    //if (a.Any((geom, id) => geom.))
    
    //    //1. hent ut geometrier
    //    //2. legg id på geometri
    //    //3. lag polygon



    //    //lage feature med polygonGEometry og exterior og interiors
    //}

    

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