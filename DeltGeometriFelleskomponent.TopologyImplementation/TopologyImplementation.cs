using System.Diagnostics;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation: ITopologyImplementation
{
    public TopologyResponse ResolveReferences(ToplogyRequest request)
        => request.Feature.Operation switch
        {
            Operation.Create => HandleCreate(request),
            Operation.Delete => HandleDelete(request),
            Operation.Update => HandleUpdate(request)
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

            if (request.Feature.References == null) return new TopologyResponse();
            if (request.AffectedFeatures == null) return new TopologyResponse();

            var affected = request.AffectedFeatures.ToDictionary(a => a.LocalId, a => a);

            var referredFeatures = new List<NgisFeature>();
            foreach (var referred in request.Feature.References)
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

            var resultPolygon = CreatePolygonFromLines(request.Feature.Type, referredFeatures, request.Feature.Centroid, out var isValid);

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
        //TODO use  Polygonizer polygonizer for all operations?

        Polygon? polygon = null;
        if (lineFeatures.Count > 1)
        {
            // Now supports  multiple linestrings and order
            Polygonizer polygonizer = new Polygonizer(extractOnlyPolygonal:true);
            foreach (var lineFeature in lineFeatures)
            {
                polygonizer.Add(lineFeature.Geometry);
            }

            if (polygonizer.GetPolygons().Count > 0)
            {
                polygon = (Polygon)polygonizer.GetPolygons().First();
                isValid = polygon.IsValid;
                if (centroid != null)
                {
                    var inside = polygon.Contains(centroid);
                    Console.WriteLine(": Point is inside polygon:{0}", inside);
                    isValid = inside;
                }
            }
            else
            {
                isValid = false;
            }
            
            var cutEdges = polygonizer.GetCutEdges();
            var dangels = polygonizer.GetDangles();

            if (cutEdges.Count > 0 ) Console.WriteLine("cutEdges.Count:{0}",cutEdges.Count);
            if (dangels.Count > 0) Console.WriteLine("dangels.Count:{0}", dangels.Count);

        }
        else
        {
            var line = lineFeatures.First();

            var linearRing = new LinearRing(line.Geometry?.Coordinates);

            polygon = new Polygon(linearRing);
            //var polygon = new Polygon(linearRing);
            isValid = polygon.IsValid;
        }

        var lokalId = Guid.NewGuid().ToString();

        var polygonFeature = new NgisFeature()
        {
            Geometry = polygon,
            LocalId = lokalId,
            Operation = Operation.Create,
            Type = type,
            References = lineFeatures.Select(a => a.LocalId).ToList(),
        };

        lineFeatures.ForEach(a => a.References.Add(lokalId));

        return polygonFeature;
    }

    private NgisFeature CreateLineFromPolyon(string type, NgisFeature polygonFeature)
    {
        var lokalId = Guid.NewGuid().ToString();

        var lineFeature = new NgisFeature()
        {
            Geometry = new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates),
            Operation = Operation.Create,
            LocalId = lokalId,
            Type = type,
            References = new List<string> { polygonFeature.LocalId }
        };

        polygonFeature.References.Add(lokalId);

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